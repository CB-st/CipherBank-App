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
  AGENTS.md
  Directory.Packages.props
  scripts/sonar/provision_quality_gate.py
  CipherBank-app.ChallengePass/AGENTS.md
  CipherBank-app.ChallengePass/Configuration/ChallengePassOptions.cs
  CipherBank-app.Core/AGENTS.md
  CipherBank-app.Core/Persist/AGENTS.md
  CipherBank-app/AGENTS.md
  CipherBank-app/Resources/Styles/AGENTS.md
  CipherBank-app/Resources/Styles/Typography.xaml
  CipherBank-app.Tests/AGENTS.md
  CipherBank-app.E2ETests/AGENTS.md
  config/README.md
  config/sonar/AGENTS.md
  config/appsettings.json
  config/appsettings.Development.json
  config/appsettings.Windows.json
  config/agentic/README.md
  config/agentic/dispatch.json
  config/challenge-pass/README.md
  config/challenge-pass/challenge-pass.json
  config/network/README.md
  config/network/endpoints.json
  docs/style/README.md
  docs/agentic/README.md
  docs/agentic/MODULE_COMPOSITION.md
  docs/agentic/RESOURCE_OWNERSHIP.md
  docs/review/m3-alignment-resolution.md
  docs/review/m4-alignment-resolution.md
  docs/review/m4-agentic-foundation.md
  docs/tests/STORY_ID_MAP.md
  scripts/create-dispatch.py
  templates/AGENTS.md
  templates/README.md
  templates/dispatch/DISPATCH.md.template
  templates/dispatch/README.md
  templates/dispatch/TEMPLATE.md
  templates/dispatch/dispatch.json.template
  templates/feature/FeatureModule.cs.template
  templates/feature/README.md
  templates/feature/TEMPLATE.md
  templates/resource/FeatureResources.xaml.template
  templates/resource/README.md
  templates/resource/TEMPLATE.md
  templates/config/README.md
  templates/config/TEMPLATE.md
  templates/e2e/PageObject.cs.template
  templates/e2e/StoryTest.cs.template
  templates/e2e/TEMPLATE.md
  templates/repository/AGENTS.md.template
  templates/repository/README.md.template
  templates/repository/TEMPLATE.md
  templates/service/README.md
  templates/service/TEMPLATE.md
  templates/ui/Page.xaml.template
  templates/ui/TEMPLATE.md
  templates/ui/ViewModel.cs.template
)
for path in "${required[@]}"; do
  [[ -f "${path}" ]] || fail "missing required ${path}"
done

if ! grep -qF 'Story=${MODE_VALUE}' scripts/e2e-android.sh \
  || ! grep -qF 'preflight_filter_or_die' scripts/e2e-android.sh; then
  fail "E2E harness must use stable Story traits and fail filtered zero-test discovery"
fi

if ! grep -qF 'artifacts/' .gitignore; then
  fail "E2E artifacts directory must remain gitignored"
fi

while IFS= read -r project; do
  if grep -nE '<PackageReference[^>]*[[:space:]]Version=' "${project}" >/dev/null; then
    fail "package version found outside Directory.Packages.props: ${project}"
  fi
done < <(find . -type f \( -name '*.csproj' -o -name '*.props' -o -name '*.targets' \) \
  ! -path './Directory.Packages.props' ! -path '*/obj/*' | sort)

while IFS= read -r package; do
  if ! grep -qF "<PackageVersion Include=\"${package}\"" Directory.Packages.props; then
    fail "PackageReference has no central PackageVersion: ${package}"
  fi
done < <(grep -RhoE '<PackageReference Include="[^"]+"' . \
  --include='*.csproj' --include='*.props' --include='*.targets' \
  --exclude='Directory.Packages.props' --exclude-dir=bin --exclude-dir=obj \
  | sed -E 's/.*Include="([^"]+)"/\1/' | sort -u)

while IFS= read -r assembly_info; do
  fail "legacy assembly metadata file found: ${assembly_info}"
done < <(find . -type f -path '*/Properties/AssemblyInfo.cs' ! -path '*/obj/*' | sort)

while IFS= read -r source; do
  [[ "${source#./}" == "CipherBank-app.Core/Persist/Sql/LocalDbSql.cs" ]] && continue
  if grep -nE '(CommandText[[:space:]]*=|FromSqlRaw|ExecuteSqlRaw)' "${source}" >/dev/null; then
    fail "raw SQL outside the compatibility object: ${source}"
  fi
done < <(find CipherBank-app.Core -type f -name '*.cs' ! -path '*/obj/*' | sort)

if grep -RInE '\b(IProductApi|MockProductApi|MockPublicQuoteService|AppSessionDeps)\b' \
  CipherBank-app.Core CipherBank-app.ChallengePass CipherBank-app --include='*.cs' >/dev/null; then
  fail "retired API-object, mock, or dependency-bag terminology remains"
fi

if grep -RInE '(BackgroundColor|TextColor|Color|Stroke|Brush)="#[[:xdigit:]]{6,8}"' \
  CipherBank-app/Views --include='*.xaml' >/dev/null; then
  fail "literal color found in a view; use a semantic resource from Colors.xaml"
fi

if grep -RIn 'FontFamily=' CipherBank-app/Views --include='*.xaml' >/dev/null; then
  fail "page-local font family found; use a semantic style from Typography.xaml"
fi

if grep -RInE '(Color\.FromArgb\("#|FontFamily[[:space:]]*=)' \
  CipherBank-app/Controls --include='*.cs' >/dev/null; then
  fail "code-created control token literal found; use Colors.xaml or Typography.xaml resources"
fi

for style_key in DisplayTitle PageHeader TitleMedium SectionHeader MoneyLarge MoneyMedium Body BodyStrong Caption Eyebrow PinEntry MonoCaption; do
  if ! grep -qF "x:Key=\"${style_key}\"" CipherBank-app/Resources/Styles/Typography.xaml; then
    fail "missing typography style ${style_key}"
  fi
done

color_line="$(grep -nF 'Resources/Styles/Colors.xaml' CipherBank-app/App.xaml | cut -d: -f1)"
type_line="$(grep -nF 'Resources/Styles/Typography.xaml' CipherBank-app/App.xaml | cut -d: -f1)"
style_line="$(grep -nF 'Resources/Styles/Styles.xaml' CipherBank-app/App.xaml | cut -d: -f1)"
if [[ -z "${color_line}" || -z "${type_line}" || -z "${style_line}" \
  || "${color_line}" -ge "${type_line}" || "${type_line}" -ge "${style_line}" ]]; then
  fail "App.xaml must merge colors, typography, then component styles"
fi

if grep -RInE '"(Password|Mnemonic|PrivateKey|Seed|Token|Pin)"[[:space:]]*:' \
  config --include='*.json' >/dev/null; then
  fail "secret-shaped configuration key found under config/"
fi

if ! python3 - <<'PY'
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

failures = []
for path in pathlib.Path("config").rglob("*.json"):
    try:
        json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        failures.append(f"invalid JSON {path}: {exc}")

dispatch_path = pathlib.Path("config/agentic/dispatch.json")
if dispatch_path.is_file():
    dispatch = json.loads(dispatch_path.read_text(encoding="utf-8"))
    required_workflows = {
        "feature-slice",
        "core-service",
        "ui-flow",
        "persistence",
        "device-journey",
        "validation",
    }
    workflows = dispatch.get("workflows", [])
    workflow_ids = {workflow.get("id") for workflow in workflows}
    missing_workflows = sorted(required_workflows - workflow_ids)
    if missing_workflows:
        failures.append(f"missing agentic workflows: {', '.join(missing_workflows)}")
    for workflow in workflows:
        workflow_id = workflow.get("id", "<unnamed>")
        skill = workflow.get("skill")
        if not isinstance(skill, str) or not skill.startswith("cipherbank-"):
            failures.append(f"agentic workflow {workflow_id} has an invalid skill")
        for key in ("templates", "references"):
            for value in workflow.get(key, []):
                if not pathlib.Path(value).is_file():
                    failures.append(f"agentic workflow {workflow_id} references missing {key[:-1]} {value}")
        if not workflow.get("gates"):
            failures.append(f"agentic workflow {workflow_id} has no verification gate")

xml_patterns = ("*.csproj", "*.props", "*.targets", "*.xaml")
for pattern in xml_patterns:
    for path in pathlib.Path(".").rglob(pattern):
        if "bin" in path.parts or "obj" in path.parts or path.name.endswith(".template"):
            continue
        try:
            ET.parse(path)
        except Exception as exc:
            failures.append(f"invalid XML {path}: {exc}")

xaml_key = "{http://schemas.microsoft.com/winfx/2009/xaml}Key"
resource_files = [
    pathlib.Path("CipherBank-app/Resources/Styles/Colors.xaml"),
    pathlib.Path("CipherBank-app/Resources/Styles/Typography.xaml"),
    pathlib.Path("CipherBank-app/Resources/Styles/Styles.xaml"),
    pathlib.Path("CipherBank-app/App.xaml"),
]
resource_owners = defaultdict(list)
for path in resource_files:
    if not path.is_file():
        continue
    for element in ET.parse(path).iter():
        if xaml_key in element.attrib:
            resource_owners[element.attrib[xaml_key]].append(str(path))
for key, owners in resource_owners.items():
    if len(owners) > 1:
        failures.append(f"duplicate XAML resource {key}: {', '.join(owners)}")

references = set()
for path in pathlib.Path("CipherBank-app").rglob("*.xaml"):
    references.update(re.findall(r"StaticResource\s+([A-Za-z0-9_]+)", path.read_text(encoding="utf-8-sig")))
missing_resources = sorted(references - set(resource_owners))
if missing_resources:
    failures.append(f"missing XAML resources: {', '.join(missing_resources)}")

for failure in failures:
    print(f"STRUCTURE ERROR: {failure}", file=sys.stderr)
raise SystemExit(1 if failures else 0)
PY
then
  failures=$((failures + 1))
fi

if ! python3 - <<'PY'
from pathlib import Path

compile(Path("scripts/create-dispatch.py").read_text(encoding="utf-8"), "scripts/create-dispatch.py", "exec")
PY
then
  fail "scripts/create-dispatch.py must compile"
fi

if (( failures > 0 )); then
  exit 1
fi

echo "Repository structure validation passed."
