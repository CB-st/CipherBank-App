# Build and Tooling Configuration

Configuration files that affect build, analysis, and editor behavior.

---

## Directory.Build.props

**Location**: Repository root

Shared MSBuild properties and analyzer package references for all projects.

| Property | Value |
|----------|-------|
| LangVersion | latest |
| Nullable | enable |
| ImplicitUsings | enable |
| TreatWarningsAsErrors | true |
| EnforceCodeStyleInBuild | true |
| AnalysisLevel | latest |
| AnalysisMode | Recommended |
| EnableNETAnalyzers | true |
| AnalysisLevel | latest-recommended |

Analyzer references are versionless here; versions are centrally owned by
`Directory.Packages.props`.

- StyleCop.Analyzers 1.2.0-beta.556
- Microsoft.CodeAnalysis.NetAnalyzers 10.0.100

**WarningsAsErrors**: Security-related CA rules (CA2100, CA5350–CA5405) are promoted to errors.

---

## global.json

**Location**: Repository root

SDK version pinning:

```json
{
  "sdk": {
    "version": "10.0.101",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
```

---

## stylecop.json

**Location**: CipherBank-app/stylecop.json

StyleCop Analyzers configuration. Referenced via `AdditionalFiles` in the app csproj.

| Setting | Value |
|---------|-------|
| companyName | CipherBank |

See [EnableConfiguration](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/EnableConfiguration.md) for full setup.

---

## .editorconfig

**Location**: Repository root

Editor and analyzer conventions.

**Global**:

- indent_style = space, indent_size = 4
- end_of_line = lf, charset = utf-8
- trim_trailing_whitespace, insert_final_newline

**C#**:

- file-scoped namespaces (suggestion)
- var when type apparent (warning)
- pattern matching preferences
- naming: interfaces I*, private fields _camelCase, constants PascalCase, async *Async
- IDE0005 (remove usings), IDE0051/0052 (unused members) as warnings
- Security: CA2100, CA2109, CA5350, CA5351, CA5359 as errors

**XML/XAML/csproj**: indent_size = 2

**JSON**: indent_size = 2

**YAML**: indent_size = 2

**Markdown**: trim_trailing_whitespace = false

---

## qodana.yaml

**Location**: Repository root

Qodana analysis configuration for CI.

| Setting | Value |
|---------|-------|
| version | 1.0 |
| ide | QDNET |
| profile.name | qodana.starter |

Optional: `bootstrap`, `plugins`, `include`/`exclude` for inspections.

---

## .gitignore

**Location**: Repository root

**Build**: bin/, obj/, Debug/, Release/, out/, log/

**IDE**: .vs/, .idea/, .cursor/

**NuGet**: *.nupkg, packages/

**User**: *.user, *.suo, .DS_Store, etc.

**Test**: TestResult.xml, coverage*.json, coverage*.xml

Top-level Markdown is tracked. Generated reports belong under ignored output or
artifact directories rather than being hidden through a broad Markdown rule.

---

## Directory.Packages.props

**Location**: Repository root

Owns every NuGet version through central package management. Project files state
only which packages they consume. `CentralPackageTransitivePinningEnabled` keeps
transitive resolution deterministic.

---

## Runtime config/

Runtime defaults are grouped by security, dispatch, network, persistence, and UI theme.
Each theme has a README, JSON defaults, a typed options class, startup validation,
and DI binding. See [`../../config/README.md`](../../config/README.md).
