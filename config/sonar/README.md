# Sonar quality-gate policy

Sonar is the only source of truth for the quality gate. There is no
checked-in copy for CI to verify against, and no repo-side script that
pushes conditions to the server..

The earlier `quality-gate.yaml` + Python verifier, and later
`provision_quality_gate.py`, were retired so this repo does not keep a
second rulebook that drifts from the live gate.

## CI's role: wait, then fail closed

`.github/workflows/sonar.yml` runs the scanner, polls
`api/ce/task` + `api/qualitygates/project_status`, and fails the `sonar`
job when the gate status is `ERROR`. Merge blocking is that job plus
Sonar's own PR check (decoration).

Coverage for new code comes from Coverlet OpenCover produced by:

- `CipherBank-app.Tests` (Core unit tests)
- `CipherBank-app.Analyzers.Tests` (structure-analyzer tests)

Those reports are the coverage job's handoff into the scan. A missing
OpenCover file leaves `new_coverage` at 0% and reds the gate.

Scanner source exclusions are limited to generated/build output, scanner reports,
editor metadata, scripts, and the out-of-stack design handoff. Do not add
`Persist/Migrations` or other product Core paths.

`sonar.coverage.exclusions` is frozen at Platforms, Resources, `*Tests*`, scripts,
and `design_handoff_cipherbank`. Do not add entries; cover product code instead.
`SonarCoverageExclusionTests` locks that exact list.

## Local feedback: SonarQube for IDE, Connected Mode

For fast local feedback that uses the *same* rules and gate as CI, bind
SonarQube for IDE (formerly SonarLint) to this project in Connected Mode
rather than relying on a local analyzer config that can drift from the
server:

1. Install "SonarQube for IDE" (VS Code, Visual Studio, or your JetBrains
   IDE all ship an equivalent extension).
2. Add a connection to `https://sonar.cipherbank.money` with your own user
   token (never a project/global token — Connected Mode requires a
   personal user token). VS Code users: a starting connection binding is
   already committed at `.vscode/settings.json`; you still need to add
   your personal token under your own user settings
   (`sonarlint.connectedMode.connections.sonarqube`), which is
   intentionally *not* committed.
3. Bind the project to `CB-st_CipherBank-App_59d7f589-fd7d-4064-9687-e720f9b3443c`.
4. From the IDE's Connected Mode panel, use "Share Connected Mode
   Configuration" to export `.sonarlint/connectedMode.json` (or the
   solution-named variant your IDE uses) and commit it, so teammates on
   other IDEs get an autobind suggestion instead of repeating steps 1–3
   from scratch. That export is IDE-generated — don't hand-write it.

Connected Mode is feedback, not the gate: it doesn't replace the server
analysis in CI, which sees full history, coverage, and PR context that a
local analysis doesn't.
