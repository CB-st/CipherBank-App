# SonarQube and analyzability contract

This scope owns code whose pull requests are evaluated by SonarQube.

## Construction rules

- Read `.compliance/docs/SONARQUBE-DEVELOPMENT-STANDARD.md` before adding or substantially changing a function or object.
- Keep each function responsible for one named outcome; use guard clauses and small pure decisions to keep valid flow shallow.
- Separate orchestration from validation, authorization, parsing, policy, persistence, and transport mapping.
- Keep object invariants enforceable at construction and dependencies explicit through narrow constructor-injected ports.
- Treat Sonar C# rule S3776 at the method level as authoritative. Do not estimate or suppress complexity to satisfy a metric.
- Treat every new vulnerability as actionable and every new Security Hotspot as requiring contextual review.

## Required evidence

- Add characterization tests before changing legacy behavior.
- Test every meaningful branch, guard, expected failure, and security rejection introduced or changed.
- Run the repository's Sonar begin/build/test/end workflow and verify the PR quality gate.
- Do not claim a pass from the overlay scanner, a local IDE result, or coverage generation alone.

## Suppressions

Never add broad `NoWarn`, analysis exclusions, or disabled rules. A narrow suppression must state the rule key, safety rationale, evidence, owner, and revisit condition.
