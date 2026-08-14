# Sonar quality-gate policy

Sonar is the only source of truth for the quality gate. There is no
checked-in copy for CI to verify against — see PR #33 for why the earlier
`quality-gate.yaml` + `verify-sonar-quality-gate.py` pair was retired: it
meant hand-keeping two rulebooks (the live gate on
https://sonar.cipherbank.money and this repo's mirror of it) in sync, and
that always drifts.

## Definition: `scripts/sonar/provision_quality_gate.py`

The gate ("CipherBank New Code Gate") is defined once, in
[`scripts/sonar/provision_quality_gate.py`](../../scripts/sonar/provision_quality_gate.py),
and pushed to the server through Sonar's own Web API. That script is the
versioned record of what the gate is supposed to be — the same role the old
YAML played — but it *acts* on the server instead of being diffed against
it. Run it with an admin token (`SONAR_ADMIN_TOKEN`, not CI's `SONAR_TOKEN`)
whenever the gate needs to change:

```bash
export SONAR_HOST_URL='https://sonar.cipherbank.money'
export SONAR_ADMIN_TOKEN='...'   # needs 'Administer Quality Gates'
export SONAR_PROJECT_KEY='CB-st_CipherBank-App_59d7f589-fd7d-4064-9687-e720f9b3443c'

python3 scripts/sonar/provision_quality_gate.py --dry-run   # preview
python3 scripts/sonar/provision_quality_gate.py             # apply
```

## Live set (PR #33)

Duplicated-line density and violations on new code. Coverage, the
reliability/security/maintainability ratings, security-hotspot review, and
blocker/critical issue counts are written into the script as a deferred set
(`DEFERRED_CONDITIONS`) but not applied by default — pass
`--include-deferred` once the team is ready to turn them on, in the same
change that updates this file.

## CI's role: wait, don't verify

`.github/workflows/sonar.yml` runs the scanner, polls
`api/ce/task` + `api/qualitygates/project_status`, and posts the result to
the job summary — this is "wait for the server's gate result," per the PR
discussion, and it's unchanged. What's gone is the extra step that used to
diff the fetched result against `quality-gate.yaml`: there's nothing left to
diff against, so CI just reports what Sonar says. Actual merge blocking is
Sonar's own PR check (decoration), which should be a required status check
on the branch protection rule — see
[`.compliance/docs/SONARQUBE-SETUP.md`](../../.compliance/docs/SONARQUBE-SETUP.md#pull-request-integration).

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
