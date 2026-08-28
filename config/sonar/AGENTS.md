# SonarQube and analyzability contract

This directory owns gate *ownership* documentation; every project in this
repo is in scope for the construction rules below, since every PR is
evaluated by Sonar.

## Gate ownership

- Never reintroduce a repo-side copy of the gate conditions (YAML, Python,
  or otherwise) that CI verifies against or pushes to the server.
- Repository-structure rules (CPM, AssemblyInfo, Core SQL, retired names)
  live in `CipherBank-app.Analyzers` and run on every `dotnet build`. Sibling
  product projects are scanned via additional files so Linux CI does not need
  to compile the MAUI host.

## Construction rules

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
- Run `.github/workflows/sonar.yml` and verify the PR quality gate.
- Do not claim a pass from a local IDE result or coverage generation alone.
  SonarQube for IDE Connected Mode is fast local feedback, not a substitute
  for the server gate.

## Suppressions

Never add broad `NoWarn`, analysis exclusions, or disabled rules. A narrow
suppression must state the rule key, safety rationale, evidence, owner, and
revisit condition.
