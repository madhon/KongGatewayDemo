---
name: dotnet-security-review
description: Use when reviewing a .NET / ASP.NET Core service, pull request, or diff for security issues — authorization gaps, injection, insecure crypto, secret leakage, unsafe deserialization — before merge or deploy.
---

# .NET Security Review

Directs the agent to perform a lens-by-lens security review of an ASP.NET Core service or PR, rating each finding by severity and exploitability and producing a concrete, API-named fix for every issue found.

## When to use

- A PR touches auth, data access, serialization, crypto, secret handling, or external calls in an ASP.NET Core service.
- A new endpoint, controller, or middleware is being added that accepts untrusted input.
- A pre-deploy security gate is required for a .NET service targeting any environment.
- Reviewing a diff where the change surface is too large to hold in one read.

## Process

1. **Scope the change and map trust boundaries.** Identify every external-input entry point (route params, query strings, request bodies, headers, file uploads), the authentication surface, and outbound calls. Note which actions are state-changing vs read-only.
2. **Confirm default-deny authorization.** Verify a fallback policy (`RequireAuthenticatedUser`) is registered in `AddAuthorization` and that no sensitive controller or action is silently reachable via `[AllowAnonymous]`.
3. **Walk the checks lens by lens** (authorization → injection → deserialization → crypto → secrets → transport → CORS → antiforgery/headers). Treat each lens as a separate pass; do not skip lenses because earlier findings were found.
4. **Rate each finding by severity and exploitability.** Use Critical / High / Medium / Low. A Critical finding blocks merge. Rate by: data sensitivity, authentication prerequisite, and whether the attacker input is directly reachable.
5. **Give a concrete fix per finding, naming the .NET API to use.** "Replace `FromSqlRaw` string-concat with `FromSqlInterpolated` (or pass `SqlParameter` objects)" is acceptable; "sanitize input" is not.
6. **Re-scan for the same class of issue elsewhere in the service.** If IDOR appears in one handler, grep the project for the same pattern in sibling handlers. Security issues are rarely singleton.

## .NET / Azure checks

- **`[Authorize]` / `[AllowAnonymous]` coverage and default-deny.** Confirm every controller and action that mutates state or returns sensitive data bears `[Authorize]` (or an explicit policy via `[Authorize(Policy = "…")]`). Confirm `AddAuthorization(opts => opts.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())` is wired. Flag any `[AllowAnonymous]` on POST/PUT/PATCH/DELETE endpoints — intentional or accidental?
- **Broken object-level authorization / IDOR.** In each handler that accepts a resource id (route, query, body), confirm the handler loads the resource and asserts `resource.OwnerId == currentUserId` (or checks an equivalent claim/scope) before returning or mutating it. Fetching `_db.Orders.FindAsync(id)` with no ownership predicate after `[Authorize]` is IDOR.
- **Overposting / mass assignment.** Check whether `[FromBody]` binds directly to an EF entity class. If so, the caller can overwrite `IsAdmin`, `OwnerId`, or other server-managed fields. Require a dedicated request DTO; map to the entity explicitly (or use `_mapper.Map<Entity>(dto)` with a profile that excludes protected fields).
- **SQL injection via `FromSqlRaw` / `ExecuteSqlRaw`.** Any call of the form `context.Set<T>().FromSqlRaw($"SELECT … WHERE id = {userInput}")` or string concatenation is injectable. Require either `FromSqlInterpolated` (which parameterizes the interpolated holes automatically) or explicit `SqlParameter` / `DbParameter` objects. LINQ queries compiled to SQL are safe; flag only raw-SQL APIs.
- **Unsafe deserialization.** Flag any use of `BinaryFormatter` (deprecated and exploitable for remote code execution) or `NetDataContractSerializer`. In Newtonsoft.Json / Json.NET, flag `TypeNameHandling.All` (or `Auto`) in `JsonSerializerSettings` without a custom `ISerializationBinder` allowlist — this enables gadget-chain RCE. Prefer `System.Text.Json` (no polymorphic type-name resolution by default).
- **Cryptographic weaknesses.** Flag `MD5.Create()` or `SHA1.Create()` used for password hashing, HMAC verification, or certificate fingerprinting. Flag hardcoded `byte[] key` / `byte[] iv` literals passed to `Aes.Create()` or `TripleDES.Create()`. Flag any hand-rolled password hasher; require `IPasswordHasher<T>` from ASP.NET Core Identity or `Microsoft.AspNetCore.DataProtection` (`IDataProtectionProvider`).
- **Secret leakage.** Scan `appsettings*.json` for connection strings with passwords, `ClientSecret`, `ApiKey`, or SAS tokens. Confirm no `_logger.Log…($"token={token}")` or `_logger.Log…(user.PasswordHash)` calls. In Azure, secrets must live in Key Vault and be consumed via `AddAzureKeyVault` / `DefaultAzureCredential` — not as plain environment variables holding raw secrets.
- **Transport and outbound call safety.** Flag `HttpClientHandler` or `SocketsHttpHandler` instances where `ServerCertificateCustomValidationCallback` returns `true` unconditionally (disables TLS validation). Flag outbound HTTP calls built from user-supplied URLs without allowlisting the scheme and host — this enables SSRF. Require `IHttpClientFactory`-typed clients with a fixed `BaseAddress`.
- **CORS misconfiguration.** Flag `AllowAnyOrigin()` chained with `AllowCredentials()` — this is rejected by the spec and means the CORS policy silently fails, or in older middleware versions, it leaks credentials. Policies must name explicit origins (`WithOrigins("https://app.example.com")`) when credentials (cookies or `Authorization` headers) are sent.
- **Antiforgery, security headers, and exception detail.** For cookie-authenticated apps, confirm `AddAntiforgery` is registered and `ValidateAntiForgeryToken` (or `AutoValidateAntiForgeryToken`) is applied to state-changing endpoints. Confirm `app.UseExceptionHandler("/error")` is used in production — not `app.UseDeveloperExceptionPage()`. Confirm `Strict-Transport-Security`, `X-Content-Type-Options`, and `X-Frame-Options` headers are emitted (via middleware or Azure Front Door / App Service managed headers).

## Red flags

| Signal | Why it matters |
|--------|----------------|
| `context.Orders.FromSqlRaw($"… WHERE Id = {id}")` | String-interpolated raw SQL is directly injectable; the `{id}` hole receives un-parameterized user input. |
| `AllowAnyOrigin().AllowCredentials()` | Violates the CORS spec and can expose authenticated responses to attacker-controlled origins depending on browser / middleware version. |
| `BinaryFormatter` in any serialization path | Enables unauthenticated remote code execution via deserialization gadget chains; .NET itself marks the type obsolete-as-error since .NET 9. |
| `ServerCertificateCustomValidationCallback = (_, _, _, _) => true` | Disables TLS certificate validation on outbound calls, enabling man-in-the-middle interception with no warning. |
| `[AllowAnonymous]` on a POST/PUT/DELETE endpoint | Bypasses the fallback policy; any authentication requirement on that action is silently dropped. Must be intentional and documented. |
| Connection string with `Password=` literal in `appsettings.json` | Secrets committed to source control are permanently exposed in git history even after removal; rotate immediately and move to Key Vault. |
| `TypeNameHandling.All` or `TypeNameHandling.Auto` in `JsonSerializerSettings` | Allows the caller to control which .NET type is deserialized, enabling gadget-chain RCE against any Newtonsoft.Json-based endpoint. |
| `_db.FindAsync(id)` with no ownership predicate after `[Authorize]` | Authenticated but not authorized — any logged-in user can access any other user's resource by guessing or enumerating ids (IDOR). |
| `MD5.Create()` used to hash passwords | MD5 is cryptographically broken; preimage attacks and rainbow tables make stored hashes trivially reversible. Use `IPasswordHasher<T>`. |
| `new byte[] { 0x00, … }` hardcoded as AES key or IV | A static key stored in source code is extractable by anyone with repo access; rotate and move to `IDataProtectionProvider` or Key Vault. |

## Example

See [`examples/dotnet-security-review/`](../../examples/dotnet-security-review/).

## Related skills

- [secrets-config-audit](../secrets-config-audit/SKILL.md) — use for deeper focus on secret handling, Key Vault wiring, and config-layer assignment.
- [threat-model-endpoint](../threat-model-endpoint/SKILL.md) — use to enumerate per-endpoint STRIDE threats and mitigations before or after a security review.
- [auth-flow-review](../auth-flow-review/SKILL.md) — use for deeper authn/z review covering token validation, policies, and cookie hygiene.
