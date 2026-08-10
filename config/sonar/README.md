# SonarQube Configuration

The workflow supplies scanner-side settings and waits for the server quality gate.
The quality gate itself is a SonarQube server object and must be assigned to the
project by a Sonar administrator. `quality-gate.yaml` is the reviewable source of
truth for that server-side setup.

The intended gate applies to **new code** so legacy debt can be reduced without
globally suppressing rules:

- reliability, security, and maintainability ratings: A;
- security hotspots reviewed: 100%;
- coverage: at least 80%;
- duplicated lines: at most 3%;
- no blocker or critical issues.

Scanner source exclusions are limited to generated/build output, scanner reports,
editor metadata, and the out-of-stack design handoff. Platform sources and MAUI
resources remain visible to analysis. Platform adapters and tests are excluded
from coverage calculation only; interfaces and production services are not
CPD-excluded.

Do not store `SONAR_TOKEN` here. Configure it as a repository secret; configure
`SONAR_HOST_URL` and `SONAR_PROJECT_KEY` as repository variables.
