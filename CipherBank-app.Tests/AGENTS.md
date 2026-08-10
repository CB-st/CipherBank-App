# Unit Test Contract

- Test externally visible behavior through public interfaces. Use Moq for a small
  collaborator contract; use an in-memory implementation when stateful behavior
  is itself under test.
- One test owns its database and temporary path. Never share an on-device database
  or secure-store state across tests.
- Every bug fix gets a regression test. Every configuration options class gets a
  default/binding validation test.
- Architecture and repository-structure tests are merge gates, not documentation.
- Avoid timing-only assertions. Synchronize concurrent tests with tasks, gates, or
  injected schedulers.
