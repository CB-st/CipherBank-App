# M3 alignment resolution

Scope: the supplied M3 stack forward-ported over the reviewed M2 architecture.
Relative to the original M2 base, M3 adds 86 files and changes 32 while retaining
the ChallengePass A1/A2 security corrections carried by M2.

| Area | M3 result |
| --- | --- |
| Feature preservation | Retained M3 onboarding, backup/restore, custody, home, convert, send/pay/receive, profile/vault, POS lab, native NFC, public quote, product HTTP, and PQ HTTP paths |
| Central packages | Forward-ported central package management and added the Android biometric package to `Directory.Packages.props` |
| Assembly metadata | Preserved project-owned metadata; the reintroduced Core `AssemblyInfo.cs` remains removed |
| DI and client boundaries | Forward-ported `IProductClient`, `InMemoryProductClient`, focused session coordination, Core options registration, and constructor-injected Cora/EMV dependencies into M3 |
| Dispatch and persistence | M3 uses the shared `TaskScheduler` + `PriorityQueue` dispatcher, EF Core repositories, and the single compatibility SQL object |
| Network configuration | Extracted hard-coded API/WebSocket environment defaults into typed, validated `config/network` options; user selections remain preferences |
| Design system | Preserved the M3 Cora palette and Manrope/Space Mono assets while moving all page typography and code-created control colors to semantic resources |
| Templates | Extended UI/repository scaffolds with service/adapter and configuration `README.md` + `TEMPLATE.md` packages |
| Structural enforcement | Carries the package, assembly, SQL, retired-name, config, XAML resource, literal-color, and page-local typography checks forward into M3 |

## Verification expectation

Run `scripts/validate-structure.sh`, repository lint, the complete unit suite,
the ChallengePass build, and the MAUI Android build before merge. A light/dark,
large-text visual pass is required for the new M3 pages and Cora controls.

The repository license remains an owner decision and is unchanged.
