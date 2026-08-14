## Modernization and compliance

This repository uses the `.compliance/` overlay for staged .NET 10 modernization.

Start with:

- `.compliance/reports/compliance-report.md`
- `.compliance/docs/MIGRATION-PLAYBOOK.md`
- `.compliance/docs/INTENT-TRANSLATION.md`
- `.compliance/docs/TESTING-PLAYBOOK.md`
- `.compliance/docs/SONARQUBE-DEVELOPMENT-STANDARD.md`
- `.compliance/docs/SONARQUBE-SETUP.md`
- `.compliance/docs/BRANCHLESS-PROGRAMMING.md`

Run the gate with `./.compliance/scripts/verify-compliance.sh`. Audit mode installs guidance without enforcing compiler settings; enforce mode enables the repository's .NET 10 compiler and analyzer contract.

Run the configured Sonar begin/build/test/end workflow with `.compliance/scripts/run-sonar-analysis.sh`; the completed server quality gate is the authoritative Sonar result.
