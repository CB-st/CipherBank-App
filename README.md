# CipherBank-App

.NET 10 **MAUI** shipping Shell for CipherBank (Android-first on Linux hosts). Expo / `design_handoff_cipherbank/` is **not** on the MAUI merge path.

## Start here

| Doc | Role |
|-----|------|
| [AGENTS.md](AGENTS.md) | Coding standards, E2E harness rules, **Sonar typology / stages / missteps / checklists** |
| [docs/README.md](docs/README.md) | Documentation index |
| [docs/BUILD_LOG.md](docs/BUILD_LOG.md) | What shipped and how layers connect |
| [docs/SONAR_GATE.md](docs/SONAR_GATE.md) | Fix vs soften policy for the Sonar gate |
| [docs/SONAR_STRUCTURAL_PLAN.md](docs/SONAR_STRUCTURAL_PLAN.md) | Stage 2 one-type-per-file plan + callers |

## Sonar (summary)

Clear **new-code** gate noise in three stages — mechanical + CRITICAL → planned SA1402/SA1649 splits → medium/minor/info. Full typology, common missteps (Shell compile gate, Story-trait filters, WNAE discipline, cross-PR duplication comments), and a pre-push checklist live in **[AGENTS.md](AGENTS.md#sonar-typology-stages-and-hard-won-methods)**.

- Host: https://sonar.cipherbank.money  
- Prefer CI artifact `sonar-context-<sha>` when PR annotation is missing.

## Quick verify

```bash
source scripts/lib/android-env.sh
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj /p:CollectCoverage=false
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android
# E2E scaffold on M1b+ (device + Appium):
./scripts/e2e-android.sh --all
# Story/wave filters need [Trait("Story", …)] Facts — M4 only (prototype/maui-m4 / PR #23):
# ./scripts/e2e-android.sh --wave account
```

## Stacked prototype PRs

Formal GitHub stack: `maui-m1a` (#25) → `m1b` (#26) → `m2` (#21) → `m3` (#22) → `m4` (#23). Fix smells on the earliest owning layer, then merge up.
