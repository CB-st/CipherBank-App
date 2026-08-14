# SonarQube setup and verification

This overlay does not install a server, change a shared quality profile, or add analyzer packages automatically. SonarScanner for .NET downloads the analyzer configuration selected by the server during its begin/build/end sequence. Keeping that configuration server-owned prevents a local NuGet analyzer version from silently diverging from the pull-request gate.

## One-time project setup

1. Create or select the SonarQube project.
2. Assign the built-in **Sonar way** C# quality profile, or a documented profile derived from it.
3. Assign a quality gate with the new-code conditions in `SONARQUBE-DEVELOPMENT-STANDARD.md`.
4. Define the project's new-code period (for example, the previous version or reference branch) consistently across repositories.
5. Create a token with analysis-only permissions and store it in the CI secret store as `SONAR_TOKEN`.
6. Store the server URL and project key as `SONAR_HOST_URL` and `SONAR_PROJECT_KEY`.
7. Install `dotnet-sonarscanner` and a supported .NET coverage tool in CI using pinned, reviewed tool versions.

Do not commit tokens, pass them as ordinary build properties, or print them in diagnostic logs.

## Required analysis order

The Scanner for .NET analysis lifecycle is:

1. scanner **begin**;
2. restore and build;
3. tests and coverage report generation;
4. scanner **end**;
5. wait for and enforce the server quality-gate result in CI.

Use `.compliance/scripts/run-sonar-analysis.sh` as a portable template. It intentionally requires the solution path and environment values. It does not guess a server or project identity.

```bash
export SONAR_HOST_URL='https://sonarqube.example'
export SONAR_PROJECT_KEY='company.product'
export SONAR_TOKEN='from-secret-store'
export SONAR_SOLUTION='Product.slnx'
./.compliance/scripts/run-sonar-analysis.sh
```

The template uses `dotnet-coverage` XML and passes it with `sonar.cs.vscoveragexml.reportsPaths`. If the repository standardizes on OpenCover or another supported tool, change both the collector and matching scanner report-path property together. A produced file is not enough—the Sonar analysis log must confirm that the report was imported.

## Pull-request integration

Configure the CI provider's SonarQube integration/decoration so the PR reports the quality gate. Fetch enough Git history for new-code and blame calculation; shallow history can reduce analysis fidelity. Make the quality gate a required merge check.

Do not run `dotnet build --no-incremental` before the begin step and reuse its outputs. The build analyzed by Sonar must occur between begin and end.

## Local connected analysis

IDE connected mode is useful for fast feedback, but it does not replace server analysis. The server gate sees the complete repository, coverage, duplication, branch/PR context, and the server's active profile.

## Verification checklist

- [ ] Begin output identifies the expected server, project key, and active profile.
- [ ] Build occurs after begin and succeeds without analyzer-loading errors.
- [ ] Tests pass and the coverage file exists.
- [ ] End output confirms coverage import and analysis upload.
- [ ] The server completes background processing successfully.
- [ ] PR decoration reports the expected new-code period.
- [ ] Zero new issues, 100% reviewed new Hotspots, at least 80% new-code coverage, and at most 3% new duplication.
- [ ] Method-level S3776 findings are resolved; no aggregate Cognitive Complexity gate was added.
- [ ] Every accepted issue/suppression has review evidence.

Official Scanner for .NET guidance: <https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/scanners/dotnet/using>

Official .NET coverage guidance: <https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/test-coverage/dotnet-test-coverage>
