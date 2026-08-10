# PR #25 M1a Review Resolution

This pass clusters the unresolved review threads by behavior so fixes remain
traceable without turning conversational comments into arbitrary code churn.

| Review cluster | Resolution |
| --- | --- |
| Sonar/coverage access | Coverage runs for every non-draft PR; Sonar runs for all same-repository PRs and protected pushes. Forks cannot receive `SONAR_TOKEN`. AI context remains allowlisted because it is cost-bearing and writes Actions state. |
| Sonar artifact flow | Kept upload/download because `needs` transfers job status and outputs, not files. Removed duplicate report copies, restored explanatory comments, removed interface CPD exclusions, and made the scanner wait for the quality gate. |
| C# style and chart math | Enforced `var` when the RHS makes the type apparent, restored `First`/`Last`, documented parameters and transformations, collapsed chart overloads into optional parameters, used `Vector2` for rendered points, and centered zero-span axes rather than substituting a span of one. |
| Central packages / assembly metadata | Added `Directory.Packages.props`, removed every project-level package version, moved CLS/COM attributes into `CipherBank-app.Core.csproj`, and removed legacy `AssemblyInfo.cs`. |
| Dispatch | Replaced list sorting with `PriorityQueue`; dispatch is performed through an injected `TaskScheduler`, with configured bounded concurrency and FIFO sequence ordering. |
| DI and test doubles | Removed `AppSessionDeps`; constructors receive interfaces directly. EMV and crypto now have injectable interfaces. Renamed `IProductApi` to `IProductClient`, renamed the stateful fixture to `InMemoryProductClient`, and replaced a large failure stub with Moq. |
| Persistence | Added EF Core SQLite model/repositories. Scattered SQL was removed; pre-EF schema repair and sensitive-column scrubbing are owned only by `LocalDbSql`. |
| Framework APIs | `Math.Sign` now selects flick direction; existing `Math.Clamp`, `TimeProvider`, async APIs, and framework collections are retained instead of local equivalents. |
| Configuration | Added typed, validated security/dispatch/persistence/UI themes under `config/`; Cora copy and crypto work factors are no longer hard-coded in their services. |
| Documentation | Added root and subtree `AGENTS.md` contracts, implementation templates, descriptive auth enum summaries, carousel rationale, and a signed-index rationale for BCL-compatible array indices. |

## Intentionally unresolved

The licensing thread requires an owner decision. The repository currently has no
declared license, so this pass does not guess between MIT, BSD, or another license
and does not mass-change copyright headers. Record the chosen license in a focused
follow-up before resolving that thread.

The `REE...`/eye-only comments and praise-only comments do not state a testable
change and are treated as conversational, not implementation requirements.
