#!/usr/bin/env bash
set -euo pipefail

base_ref="${1:?usage: verify-generated-migrations.sh <base-ref>}"
root="$(git rev-parse --show-toplevel)"
cd "$root"

migrations='CipherBank-app.Core/Persist/Migrations'
snapshot="${migrations}/CipherBankDbContextModelSnapshot.cs"
mapfile -t added < <(
  git diff --name-status "${base_ref}...HEAD" -- "${migrations}" |
    awk '$1 == "A" && $2 ~ /[0-9]{14}_.+\.cs$/ && $2 !~ /Designer\.cs$/ { print $2 }'
)
mapfile -t modified < <(
  git diff --name-status "${base_ref}...HEAD" -- "${migrations}" |
    awk '$1 == "M" && $2 != "'"${snapshot}"'" && $2 !~ /20260817134948_InitialCreate(\.Designer)?\.cs$/ { print $2 }'
)

if ((${#modified[@]} > 0)); then
  printf 'Existing migration artifacts are generator-owned and cannot be edited:\\n%s\\n' "${modified[*]}" >&2
  exit 1
fi
if ((${#added[@]} > 1)); then
  echo "Only one generated migration may be added per PR." >&2
  exit 1
fi

dotnet ef migrations has-pending-model-changes \
  --project CipherBank-app.Core/CipherBank-app.Core.csproj \
  --startup-project CipherBank-app.Tests/CipherBank-app.Tests.csproj

if ((${#added[@]} == 0)); then
  exit 0
fi

expected="$(mktemp -d)"
worktree="$(mktemp -d)"
trap 'git worktree remove --force "$worktree" >/dev/null 2>&1 || true; rm -rf "$expected" "$worktree"' EXIT

migration="${added[0]}"
designer="${migration%.cs}.Designer.cs"
name="$(basename "${migration%.cs}")"
name="${name#*_}"
cp "$migration" "$expected/"

git worktree add --detach "$worktree" HEAD >/dev/null
rm "$worktree/$migration" "$worktree/$designer"
git -C "$worktree" show "${base_ref}:${snapshot}" >"$worktree/$snapshot"
dotnet restore "$worktree/CipherBank-app.Tests/CipherBank-app.Tests.csproj" >/dev/null
dotnet ef migrations add "$name" \
  --project "$worktree/CipherBank-app.Core/CipherBank-app.Core.csproj" \
  --startup-project "$worktree/CipherBank-app.Tests/CipherBank-app.Tests.csproj" \
  --output-dir Persist/Migrations >/dev/null

generated_migration="$(find "$worktree/$migrations" -maxdepth 1 -name "*_${name}.cs" ! -name "*.Designer.cs")"
python3 - "$expected/$(basename "$migration")" "$generated_migration" <<'PY'
import re
import sys
from pathlib import Path

def normalized(path: str) -> str:
    return re.sub(r"\b\d{14}\b", "<TIMESTAMP>", Path(path).read_text())

pairs = zip(sys.argv[1::2], sys.argv[2::2])
different = [(expected, actual) for expected, actual in pairs if normalized(expected) != normalized(actual)]
if different:
    for expected, actual in different:
        print(f"Generated migration drift: {expected} != {actual}", file=sys.stderr)
    raise SystemExit(1)
PY
