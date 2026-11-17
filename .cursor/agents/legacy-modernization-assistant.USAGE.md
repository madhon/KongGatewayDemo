# How to use — Legacy Modernization Assistant (agent)

**What it is.** An agent that assesses a legacy .NET codebase and produces a pragmatic, incremental modernization roadmap (strangler-fig, not big-bang) — platform, architecture, DI, async, testability — sequenced by risk and value.

**When to reach for it.** Modernizing legacy code, migrating off .NET Framework, or "where do I even start with this old codebase".

**How to use it.** Point it at the codebase (path or GitHub URL). Example prompts:
- "Assess this legacy app and give me a modernization roadmap."
- "We're on .NET Framework — how do we move to modern .NET incrementally?"
- "Where do I start untangling this monolith?"

**Get the best out of it.** Decide the destination with it first — consolidate on **.NET Framework 4.8** (modern tooling, same runtime) or **port to modern .NET** — because that changes which blockers matter. It runs a concrete .NET Framework blocker scan (System.Web, WCF, Remoting, BinaryFormatter, packages.config, classic csproj) and then hands the actual edits to the right skill: `legacy-package-migrator` (packages.config → Central Package Management), `adonet-data-access-auditor` (classic ADO.NET), `winforms-maintainer` (WinForms fixes), plus `dotnet-upgrade-agent`, `modular-monolith-generator`, `test-coverage-gap-finder`, and `async-await-auditor`.

**Try it on the sample.** `examples/legacy-pack/` ships a deliberately-broken .NET Framework solution — point the agent at it and compare against `examples/legacy-pack/VALIDATION-REPORT.md`.

**Won't do.** It won't recommend a big-bang rewrite unless there's strong justification — incremental is the default. It plans and sequences; the skills make the edits, one project per PR.
