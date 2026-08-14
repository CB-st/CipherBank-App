# Stack staged work

Cross-cutting goals that land on a later slice, not as backfills on earlier PRs.
Earlier layers may omit a row on purpose. Do not hold M1–M3 for an M4+ item.

Owner is the first slice that may introduce the work. Later slices inherit it.

## Big

| Item | Owner | Status |
| --- | --- | --- |
| AI review coverage signal: publish `cipherbank_coverage_report.txt` in the `coverage-report` artifact so `ai-context.yml` can set `coverage_present` | M4 | On M4–M8 (`498fb78`). Omitted on M1–M3. |
| Userdata pack crypto + TCP loopback **53809** (from draft #28 / #29) | M8 | Parked until M7 is ready to accept follow-ons. |
| Production Android SPKI pins (no invented hashes) | M6+ | System CA until real pins exist. |
| NuGet lock files + locked restore, after MAUI/Android lock graphs are stable | post-stack | Not an M1 half-switch. Locks and locked restore land together. |

## Small

| Item | Owner | Status |
| --- | --- | --- |
| Config overlays: `Build(environment, windowsOverlay)` + host wire | M3a / M6 | Loader on #43; `MauiProgram` on #39. |
| `scripts/validate-structure.sh` in the coverage job | M1 | On this slice. |
| Shrink Shell StyleCop / `TreatWarningsAsErrors` allowlist | M6+ | Narrow, documented, shrinking. |
| Remaining E2E waves beyond the account baseline | M7+ | Account wave stays the proven gate. |
| Full XMR address checksum | later | Needs an in-tree Monero decoder. Do not invent one. |
| Sonar Stage 2 file splits (SA1402 / SA1649) | earliest owning file | See `docs/SONAR_STRUCTURAL_PLAN.md` when that doc exists on the slice. |

## Not in this list

- SonarAnalyzer in default `dotnet build` / `Directory.Build.*`
- A checked-in `quality-gate.yaml` that CI must keep in sync with the server
- Re-committing `.compliance/` into the product repo
