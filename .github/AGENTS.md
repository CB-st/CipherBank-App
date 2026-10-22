# CI and Sonar Contract

- Coverage runs for every non-draft PR. Sonar runs for same-repository PRs and
  protected-branch pushes; fork PRs cannot receive the Sonar token.
- Cross-job files must move through immutable, commit-scoped artifacts.
- Actions are pinned to commit SHAs. Workflow permissions stay least-privilege.
- The scanner must wrap a real restore/build/test and wait for the Sonar quality
  gate. Do not exclude interfaces or production folders merely to reduce findings.
- Analyzer or CPD exclusions require a specific rationale and the narrowest path.
