---
name: ef-core-review
description: Use when reviewing Entity Framework Core usage — for performance (N+1, tracking, projections), correctness (transactions, concurrency), and security (raw-SQL injection, migrations).
---

# EF Core Review

Directs the agent to perform a systematic review of Entity Framework Core usage across query loading strategy, write correctness, raw-SQL safety, and migration hygiene, producing a severity-rated finding with a named EF Core API fix for each issue.

## When to use

- A PR introduces or modifies EF Core queries, `DbContext` configuration, or `SaveChanges` call sites.
- A service shows slow database response times and the cause may be N+1 loading or missing `AsNoTracking`.
- Raw-SQL via `FromSqlRaw` or `ExecuteSqlRaw` appears anywhere in the diff.
- A new migration is being reviewed before it runs in staging or production.

## Process

1. **Find the query hotspots and write paths.** Locate every `DbSet<T>` access, every `SaveChanges` / `SaveChangesAsync` call, and any `FromSqlRaw` / `ExecuteSqlRaw` usage. Note which queries are inside loops.
2. **Check the loading strategy.** For each navigation property access, determine whether EF Core will lazy-load (issuing a separate query per row), eager-load via `Include`, or explicitly load. Flag every place where a navigation is accessed inside a loop without a prior `Include`.
3. **Check write and transaction correctness.** Confirm multi-entity writes are wrapped in a transaction and that `SaveChanges` is called once per unit of work, not once per entity or per loop iteration. Verify concurrency tokens are present on entities that can be updated concurrently.
4. **Check raw-SQL safety.** For every `FromSqlRaw` / `ExecuteSqlRaw` call, verify the SQL string is a compile-time literal or uses only `SqlParameter` / `DbParameter` objects — never string interpolation or concatenation of user-supplied values. Prefer `FromSqlInterpolated` when interpolation is genuinely needed; it extracts each hole as a parameterized `DbParameter` automatically.
5. **Check migrations for data loss and idempotency.** Review each `MigrationBuilder` method for destructive operations (column drops, renames, type changes) that could lose data. Confirm that migrations are idempotent when generated with `--idempotent` for deployment. Check that `EnableRetryOnFailure` is configured for transient-fault resilience and that `DbContext` lifetime and pooling match the application host model.
6. **Output findings with fixes.** Rate each finding (Critical / High / Medium / Low), name the EF Core API that resolves it, and note whether there are sibling queries with the same defect that need the same fix.

## .NET / Azure checks

- **N+1 from lazy loading or missing `Include`.** Check whether `UseLazyLoadingProxies()` is enabled and whether navigation properties are accessed inside loops. A `foreach` over an `Order` list that reads `order.Customer.Name` without `.Include(o => o.Customer)` issues one `SELECT` per row. Fix with `.Include(o => o.Customer)` (eager) or `entry.Reference(o => o.Customer).LoadAsync()` (explicit, single call before the loop). Prefer projecting to a DTO with `Select` to fetch only the columns needed.
- **`AsNoTracking()` for read-only queries.** Any `DbSet<T>` query whose results are never passed to `SaveChanges` should call `.AsNoTracking()` or use `UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)` at the context level for read-heavy contexts. Tracked queries allocate change-tracking snapshots — on large result sets this is measurable GC pressure with no benefit. Note: `AsNoTracking` does not change which rows are returned; it only omits the identity map and snapshot.
- **DTO projection instead of materializing full entities.** A `.ToListAsync()` that returns `List<Order>` when the caller only needs order id and total unnecessarily fetches every column. Use `.Select(o => new OrderSummaryDto { Id = o.Id, Total = o.Total }).ToListAsync()` to push projection to the database. Returning EF entities directly from controllers also exposes unmapped columns and circular-reference serialization issues.
- **Raw-SQL injection via `FromSqlRaw` / `ExecuteSqlRaw`.** Any call of the form `context.Orders.FromSqlRaw($"SELECT … WHERE Status = '{status}'")`  or `+ userInput` is SQL-injectable. Require `FromSqlInterpolated($"SELECT … WHERE Status = {status}")` — EF Core extracts each `{…}` hole as a `DbParameter`, so the database always treats it as a bound value. For `ExecuteSqlRaw`, pass `SqlParameter` objects as the `params object[]` argument. LINQ queries are safe because EF Core always parameterizes them.
- **Client-side evaluation forced by unsupported expressions.** When a LINQ `Where` predicate contains a .NET method EF Core cannot translate (e.g., `o.Description.Contains(someRegex)` using a regex overload, or a custom extension method), EF Core 3+ throws at runtime rather than silently pulling all rows to the client. Run the query in development and confirm no `InvalidOperationException` about client-side evaluation. Rewrite using translatable members or a raw-SQL alternative.
- **`SaveChanges` inside loops.** Calling `context.SaveChangesAsync()` inside a `foreach` issues one `UPDATE` / `INSERT` round-trip per iteration and wraps each in its own implicit transaction. Accumulate all changes and call `SaveChangesAsync()` once after the loop. For very large batches, consider `ExecuteUpdateAsync` / `ExecuteDeleteAsync` (EF Core 7+) which translate to set-based SQL without loading entities.
- **Concurrency tokens and transactions for multi-entity writes.** Entities that can be updated by concurrent requests need a concurrency token: either a `[Timestamp]` / `byte[]` property mapped with `.IsRowVersion()` (SQL Server `rowversion`) or a `[ConcurrencyCheck]` scalar property. Without a token, the last writer silently wins. Multi-entity write operations that must be atomic must use an explicit `IDbContextTransaction` via `context.Database.BeginTransactionAsync()` and commit or roll back as a unit.
- **Migrations: destructive operations, idempotency, and resilience.** Review `MigrationBuilder.DropColumn`, `RenameColumn`, and column-type changes for data loss. A column drop with no preceding data-migration step loses data permanently. Confirm `context.Database.MigrateAsync()` is not called on startup in a multi-instance deployment (use a one-shot migration job instead). Confirm `EnableRetryOnFailure(maxRetryCount: 5)` is set in `UseSqlServer` / `UseNpgsql` options for transient Azure SQL / Postgres errors. Confirm `DbContext` is registered with `AddDbContext<T>` (scoped lifetime) or `AddDbContextPool<T>` (pooled, scoped, all state reset between requests) — never as a singleton, which causes cross-request state pollution.

## Red flags

| Signal | Why it matters |
|--------|----------------|
| `context.Orders.FromSqlRaw($"… WHERE Status = '{status}'")`  | String-interpolated raw SQL passes user input directly into the query; the interpolated hole is not parameterized by `FromSqlRaw`, making it trivially injectable. Use `FromSqlInterpolated`. |
| Navigation property accessed inside `foreach` with no prior `Include` | Issues one `SELECT` per loop iteration (N+1). On a list of 500 rows this is 501 round-trips; on a large dataset it is a liveness risk. |
| `.ToList()` followed by `.Where(…)` in memory | EF Core fetches every row from the database and then filters in the .NET process. Use `.Where(…).ToListAsync()` to push the predicate to SQL. |
| `await context.SaveChangesAsync()` inside a loop body | Each call opens and closes an implicit transaction. Accumulate changes first; call `SaveChangesAsync` once outside the loop. |
| Controller action returns `IEnumerable<Order>` (EF entity) directly | Exposes every column including internal fields, risks serialization cycles on navigation properties, and leaks the data model to the API contract. Project to a DTO. |
| Migration with `DropColumn` and no prior data-migration step | Drops data permanently on the next deploy. Add a data-migration migration before the destructive one, or move the data in the same migration using `migrationBuilder.Sql`. |
| No `.AsNoTracking()` on read-only queries | Every tracked entity allocates a change-tracking snapshot. On a query returning thousands of rows this wastes memory and GC time with no benefit when results are never saved. |
| `DbContext` registered as `AddSingleton<AppDbContext>` | A singleton `DbContext` is shared across all requests and across `Task.WhenAll` parallel paths. `DbContext` is not thread-safe; concurrent access corrupts its internal state map. |

## Example

See [`examples/ef-core-review/`](../../examples/ef-core-review/).

## Related skills

- [dotnet-performance-review](../dotnet-performance-review/SKILL.md) — use for broad .NET performance review beyond EF Core queries (allocations, async, caching).
- [dotnet-security-review](../dotnet-security-review/SKILL.md) — use to catch raw-SQL injection and other security issues across the full service.
