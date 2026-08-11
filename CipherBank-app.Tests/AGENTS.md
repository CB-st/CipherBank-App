# Unit Test Contract

- Characterize existing observable behavior before refactoring it.
- Test externally visible behavior through public interfaces. Use Moq for a small
  collaborator contract; use an in-memory implementation when stateful behavior
  is itself under test.
- One test owns its database and temporary path. Never share an on-device database
  or secure-store state across tests.
- Every bug fix gets a regression test that fails without the fix. Every
  configuration options class gets a default/binding validation test.
- Architecture and repository-structure tests are merge gates, not documentation.
  `RepositoryStructureTests` must keep requiring `scripts/sonar/provision_quality_gate.py`.
- Avoid timing-only assertions. Synchronize concurrent tests with tasks, gates, or
  injected schedulers.
- Do not mock EF query providers, serializers, or framework internals.

## This project

Unit tests for CipherBank-app.Core and architecture/structure checks
(`Architecture/`). Fastest tier; runs in the `coverage` job of
`.github/workflows/sonar.yml` and feeds Coverlet/OpenCover into the Sonar scan.
