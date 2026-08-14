#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repository_root"

solution="$(find . -maxdepth 2 \( -name '*.slnx' -o -name '*.sln' \) -print -quit)"
if [[ -z "$solution" ]]; then
  echo "No solution found within two directory levels." >&2
  exit 2
fi

dotnet --version
dotnet restore "$solution" --use-lock-file
dotnet format "$solution" --verify-no-changes --no-restore
dotnet build "$solution" --configuration Release --no-restore
dotnet test "$solution" --configuration Release --no-build --collect:"XPlat Code Coverage"
dotnet list "$solution" package --vulnerable --include-transitive
dotnet list "$solution" package --deprecated
