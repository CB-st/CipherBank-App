# Legacy stack comment triage → M1–M7

Harvested from legacy PRs #20–#26 (line + issue + review comments). Active work belongs on **#33–#40**; legacy heads are **DRAFT redundant**.

## Redundancy map

| Legacy | Status | Replacement |
|--------|--------|-------------|
| #20 | DRAFT redundant | #33 M1 |
| #21 | DRAFT redundant | #38 M5 |
| #22 | DRAFT redundant | #39 M6 |
| #23 | DRAFT redundant | #40 M7 |
| #24 | DRAFT redundant | #33–#40 stack |
| #25 | DRAFT redundant | #33 / #35 / #36 |
| #26 | DRAFT redundant | #37 M4 |
| #31 | DRAFT redundant | #34 docs |
| #32 | DRAFT redundant | #34 agentic |
| #28 / #29 | DRAFT **keep** | Userdata — restack (not redundant) |

## ConnorS-P / noahbraunf themes (#25 primarily)

| Theme | Disposition | New-stack evidence |
|-------|-------------|-------------------|
| Sonar for all PRs / drop actor allowlist | **Done** | M1 `#33` sonar workflow open to PRs |
| Coverage via job artifact `needs:` | **Done** | coverage → sonar jobs on stacked PRs |
| Central package management | **Done** | `Directory.Packages.props` (M1) |
| EF Core for Persist | **Done** | M2 `#35` EF + LocalDbSql |
| `IProductApi` → client naming | **Done** | `IProductClient` / `InMemoryProductClient` (M3) |
| Drop `AppSessionDeps` bag | **Done** | `ProductSessionCoordinator` + DI |
| Cora lines in config | **Done** | `config/ui/cora-lines.json` + `CoraLineProvider` |
| ChartMath docs + epsilon/zero-span | **Done** | XML docs; zero-span centers instead of dx=1 |
| PriceHistory First/Last | **Done** | Uses `First()`/`Last()` on tip |
| Options `SectionName` → `nameof(Class)` | **Won’t-fix (config keys)** | Short section keys (`"Cora"`, `"Carousel"`) match JSON; `nameof(CoraOptions)` would break bindings |
| Persist entities as `record` | **Partial / Stage 2** | Prefer EF entity mutability; revisit in Sonar Stage 2 if desired |
| License / copyright headers | **Deferred (owner)** | Still no SPDX license; headers remain CipherBank copyright |
| Moq for simulators | **Won’t-fix for now** | Explicit fakes/`IEmvExchangeSimulator` preferred over Moq dependency |
| `SyncJobScheduler` → PriorityQueue | **Done** | PriorityQueue dispatch on M2/M3 tip |
| Carousel `Math.Sign` / extract | **Done / acceptable** | Tip CarouselMath cleaned in overhaul |
| BackupQuiz signed index | **Acceptable** | Word indices are 0-based ints matching BIP39 list |
| CryptoBox constants → config | **Done** | `CryptographyOptions` |
| Top-level md gitignore | **Done / check** | M4 harness docs committed; verify `.gitignore` does not blanket `*.md` |
| AssemblyInfo → csproj | **Done** | Pack removed legacy AssemblyInfo pattern |
| ChartPoint vs PointF | **Won’t-fix** | Domain `ChartPoint` keeps T/V semantics |

## Security / ChallengePass threads (#21)

Author replies already mark wipe/`_buildGate`/fused A2/HKDF span fixes landed on former m2 tip; restacked onto **#38 M5**. No open Fix rows requiring new M5 code beyond verifying Sonar green (pass as of triage).

## Shell / E2E (#22 / #23)

Mostly bot nits + author “Fixed on tip” threads carried into **#39 / #40**. Account-wave E2E remains the proven baseline; next waves are Task 5.

## Sonar on new stack (2026-08-11)

| PR | Sonar QG | Notes |
|----|----------|-------|
| #33 M1 | pass | |
| #35 M2 | pass | |
| #36 M3 | **fixing** (`fdc2c48` DI/Wallet/stream tests) | Was 78.8% new_coverage; awaiting CI re-check |
| #37–#40 | pass | |

**Open Fix:** raise M3 new-code coverage ≥80% (prefer tests for 0% DI registration + `WalletRepository` + stream paths).

## Bot volume

~729 comments across #20–#26; majority bots (`conman` / `gifany` / `cave`) with author Fixed replies. Do not re-litigate Fixed threads on legacy heads — verify on replacement tips only.
