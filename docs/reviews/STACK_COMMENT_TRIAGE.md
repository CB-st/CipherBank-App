# Active stack comment triage (2026-08-12)

Harvest: ~205 comments across #33, #35, #43, #44, #37–#40. Most round-2/3
findings are already on tip; bots re-fired on older SHAs.

Legend: **Fixed** on tip · **Fix-now** patched this pass · **Later-layer** belongs
upstream/downstream · **Defer** needs ops/decoder · **Reply-only** already addressed

| PR | Item | Disposition | Notes |
|----|------|-------------|-------|
| #33 | Author gate on secret jobs | **Fix-now** | Coverage open; `context`/`sonar`/`review` gated on `PIPELINE_ALLOWED_AUTHOR` |
| #33 | SQLite bundle → Android | **Fixed** | `ExcludeAssets` on bundle + desktop lib in Core csproj |
| #33 | `validate-structure` / `IWalletService` | **Reply-only** | Retired pattern is `IProductApi\|MockProductApi\|AppSessionDeps` only |
| #35 | EnsureCreated missing tables | **Fixed** | `EnsureMissingModelTablesAsync` |
| #35 | Mask recompute / digit routing / legacy populate | **Fixed** | RecipientRepository + LocalDbSql |
| #43 | AesGcm blob versioning | **Fixed** | Open prefers legacy layout first, then `0x01` marker |
| #43 | PIN staging atomic / Unlock clear mnemonic | **Fixed** | Staging* + clear on fail |
| #43 | BIP39 NFKD / OpenPack continue on bad block | **Fixed** | FormKD + catch CryptographicException |
| #43 | Full XMR decode | **Defer** | Needs Monero decoder |
| #44 | Stream CTS decouple / message size bound | **Fixed** | Independent `_cts` + `MaxAccumulatedMessageBytes` |
| #44 | Preserve local idle on refresh fail | **Fixed** | Load prefs before refresh |
| #44 | Bootstrap recipient ID hash / Recipients Populate | **Fixed** | SHA256 seed + `JsonObjectCreationHandling.Populate` |
| #44 | HttpProductClient bearer / DI | **Fixed** | `ProductAuthHeaderHandler` on HttpClient |
| #37 | lint-cpp C std + fail on warn | **Fixed** | Per-TU `-std=c17`/`c++17`; nonempty tidy → fail |
| #37 | WAVE_STORIES / Story traits | **Later-layer** | Harness on M4; traits + Facts land on **M7** (#40) |
| #38 | KEM wipe / key ownership / Static dispose | **Fixed** | ChallengePass tip |
| #39 | Placeholder cert pins | **Fix-now** | Removed fake pins; system CA until real SPKIs |
| #39 | Idle after boot fail / navigate-before-wipe | **Fixed** | AppShell + AppIdleLockService |
| #39 | Convert quote invalidate / Send payee Id | **Fixed** | ViewModels |
| #39 | Clipboard inject | **Fixed** | `IAppClipboard` on ReceiveViewModel |
| #39 | TreatWarningsAsErrors | **Defer** | Shell StyleCop debt / WNAE allowlist |
| #40 | Smoke Home→Receive / PosLab nav | **Fixed** | CoraShellSmokeTests |
| #40 | Sealed wave profile / worktree `.git` file | **Fixed** | e2e-android + RepoPaths |

## Policy

- **Core product** comments → earliest of M1–M7 owning layer.
- **M8** only agentic templates + comparison/triage docs (this file).
- Do not invent Android pin hashes; do not soft-pass Sonar HIGH.
