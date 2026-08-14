# SonarQube and analyzability contract

Adapted from `.compliance/templates/AGENTS.sonar.md`. This directory owns
gate *definition*; every project in this repo is in scope for the
construction rules below, since every PR is evaluated by Sonar.

## Gate ownership (repository-specific)

- The gate lives on `https://sonar.cipherbank.money` (project key
  `CB-st_CipherBank-App_59d7f589-fd7d-4064-9687-e720f9b3443c`). There is no
  checked-in mirror of it — see `README.md` in this directory for why, and
  `scripts/sonar/provision_quality_gate.py` for how the gate gets defined.
- Never reintroduce a repo-side copy of the gate conditions that CI verifies
  against. If the gate needs to change, change it on the server via that
  script, in a reviewed PR that touches the script.
- `RepositoryStructureTests.cs` and `scripts/validate-structure.sh` both
  require `scripts/sonar/provision_quality_gate.py` to exist, the same way
  they used to require `quality-gate.yaml` — keep both in sync if this file
  moves.

## Construction rules

- Read `.compliance/docs/SONARQUBE-DEVELOPMENT-STANDARD.md` before adding or
  substantially changing a function or object.
- Keep each function responsible for one named outcome; use guard clauses
  and small pure decisions to keep valid flow shallow.
- Separate orchestration from validation, authorization, parsing, policy,
  persistence, and transport mapping.
- Keep object invariants enforceable at construction and dependencies
  explicit through narrow constructor-injected ports.
- Treat Sonar C# rule S3776 at the method level as authoritative. Do not
  estimate or suppress complexity to satisfy a metric.
- Treat every new vulnerability as actionable and every new Security Hotspot
  as requiring contextual review.

## Required evidence

- Add characterization tests before changing legacy behavior.
- Test every meaningful branch, guard, expected failure, and security
  rejection introduced or changed.
- Run the repository's Sonar begin/build/test/end workflow
  (`.github/workflows/sonar.yml`, or `.compliance/scripts/run-sonar-analysis.sh`
  locally) and verify the PR quality gate.
- Do not claim a pass from the overlay scanner, a local IDE result, or
  coverage generation alone. SonarQube for IDE's Connected Mode
  (`../.vscode/settings.json`) is fast local feedback, not a substitute for
  the server gate.

## Suppressions

Never add broad `NoWarn`, analysis exclusions, or disabled rules. A narrow
suppression must state the rule key, safety rationale, evidence, owner, and
revisit condition.
