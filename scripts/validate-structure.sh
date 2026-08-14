#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${repo_root}"

failures=0
fail()
{
  echo "STRUCTURE ERROR: $*" >&2
  failures=$((failures + 1))
}

required=(
  Directory.Packages.props
  scripts/sonar/provision_quality_gate.py
)
for path in "${required[@]}"; do
  [[ -f "${path}" ]] || fail "missing required ${path}"
done

while IFS= read -r project; do
  if grep -nE '<PackageReference[^>]*[[:space:]]Version=' "${project}" >/dev/null; then
    fail "package version found outside Directory.Packages.props: ${project}"
  fi
done < <(find . -type f \( -name '*.csproj' -o -name '*.props' -o -name '*.targets' \) \
  ! -path './Directory.Packages.props' ! -path '*/obj/*' | sort)

while IFS= read -r assembly_info; do
  fail "legacy assembly metadata file found: ${assembly_info}"
done < <(find . -type f -path '*/Properties/AssemblyInfo.cs' ! -path '*/obj/*' | sort)

while IFS= read -r source; do
  [[ "${source#./}" == "CipherBank-app.Core/Persist/Sql/LocalDbSql.cs" ]] && continue
  if grep -nE '(CommandText[[:space:]]*=|FromSqlRaw|ExecuteSqlRaw)' "${source}" >/dev/null; then
    fail "raw SQL outside the compatibility object: ${source}"
  fi
done < <(find CipherBank-app.Core -type f -name '*.cs' ! -path '*/obj/*' | sort)

if grep -RInE '\b(IProductApi|MockProductApi|AppSessionDeps)\b' \
  CipherBank-app.Core CipherBank-app CipherBank-app.Tests --include='*.cs' >/dev/null; then
  fail "retired API-object, mock, or dependency-bag terminology remains"
fi

if (( failures > 0 )); then
  exit 1
fi

echo "Repository structure validation passed."
