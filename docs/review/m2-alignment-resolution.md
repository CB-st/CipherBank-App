# M2 alignment and review resolution

Scope: M2 PR #21 (`prototype/maui-m2`) aligned with the reviewed M1a architecture from PR #25.

All 33 inline M2 review threads were resolved on the supplied M2 tip. Their security outcomes—especially exception-safe key cleanup, displaced-channel-key cleanup, and the fused A2 identity path—are preserved here.

| Area | M2 result |
| --- | --- |
| Central packages | Added ChallengePass, configuration, and local Sonar analyzer packages to `Directory.Packages.props`; removed per-project versions |
| Assembly metadata | ChallengePass metadata and CLS/COM attributes are declared in its project file; no legacy AssemblyInfo file remains |
| DI and options | ChallengePass exposes interface-based host ports and typed, validated suite selection while retaining compatible registration overloads |
| Dispatch | M2 consumes the M1a `TaskScheduler` + `PriorityQueue` bounded scheduler; ChallengePass does not create an independent dispatcher |
| Persistence | M2 consumes the M1a EF Core repositories and centralized compatibility SQL object; ChallengePass has no database dependency |
| API naming | Shared `IProductApi`/`MockProductApi`/dependency-bag terminology is retired in favor of `IProductClient`, `InMemoryProductClient`, and focused coordination |
| Configuration | Added documented `config/challenge-pass/` defaults with startup validation; configuration contains no secrets |
| Crypto review invariants | Preserved secret zeroization on exceptional paths, retired-key cleanup, and fused A2 identity/channel/pass construction |
| Sonar and structure | Reused the narrow M1a Sonar workflow, centralized analyzers, and expanded the structure gate for M2 and design-system requirements |
| Typography and templates | Split semantic typography from general controls and added copy-ready UI/repository template packages with subtree contracts |

## Verification expectation

`scripts/validate-structure.sh`, XML/JSON/YAML parsing, C# syntax parsing, the full unit suite, the ChallengePass build, and the MAUI Android build are the handoff gates. If the .NET SDK or MAUI workload is unavailable locally, CI must execute the three .NET commands before merge.

The repository license remains an owner decision and is not changed by this alignment pass.
