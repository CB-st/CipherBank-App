#!/usr/bin/env bash
set -euo pipefail

: "${SONAR_HOST_URL:?Set SONAR_HOST_URL to the SonarQube server URL.}"
: "${SONAR_PROJECT_KEY:?Set SONAR_PROJECT_KEY to the SonarQube project key.}"
: "${SONAR_TOKEN:?Set SONAR_TOKEN from a protected secret store.}"
: "${SONAR_SOLUTION:?Set SONAR_SOLUTION to the .sln or .slnx path.}"

coverage_directory="${SONAR_COVERAGE_DIRECTORY:-TestResults/SonarQube}"
coverage_path="$coverage_directory/coverage.xml"
mkdir -p "$coverage_directory"

dotnet sonarscanner begin \
  "/k:${SONAR_PROJECT_KEY}" \
  "/d:sonar.host.url=${SONAR_HOST_URL}" \
  "/d:sonar.cs.vscoveragexml.reportsPaths=${coverage_path}"

dotnet restore "$SONAR_SOLUTION" --use-lock-file
dotnet build "$SONAR_SOLUTION" --configuration Release --no-restore
printf -v test_command 'dotnet test %q --configuration Release --no-build' "$SONAR_SOLUTION"
dotnet-coverage collect \
  "$test_command" \
  --format xml \
  --output "$coverage_path"

test -s "$coverage_path"
dotnet sonarscanner end

echo "Analysis uploaded. Enforce the completed server quality-gate result in CI before merge."
