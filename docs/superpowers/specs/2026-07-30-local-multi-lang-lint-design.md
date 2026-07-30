# Local multi-language lint harness (org-ready)

**Date:** 2026-07-30  
**Repo:** CipherBank-App (CB-APP MAUI tip / stacked `prototype/maui-m*`)  
**Status:** Approved for implementation  
**Related:** [LOCAL_SONAR_LINT.md](../../LOCAL_SONAR_LINT.md), [SONAR_GATE.md](../../SONAR_GATE.md), `scripts/lint-csharp.sh`

## Goal

Give Cursor agents and developers a **single local entrypoint** that runs Sonar-aligned C# analysis already in-tree, plus portable linters for **Shell, Python, C++, and Make**, so the same workflow works when moving across CipherBank org repos (tooling Python, backend C++, build Makefiles) without waiting for those languages to appear in CB-APP.

## Non-goals

- Replacing the SonarQube server quality gate (`new_coverage`, CPD density remain CI/server).
- Softening GitHub Sonar check thresholds.
- Shipping a shared org NuGet/npm package in this PR (copyable scripts are enough).
- Enabling SonarAnalyzer on every default `dotnet build` (stays opt-in via `EnableSonarAnalyzers`).
- Installing language toolchains (compilers) — only linters / format-check tools.

## Approach

**Thin dispatcher + per-language scripts** (Approach A from brainstorming):

| Piece | Responsibility |
|-------|----------------|
| `scripts/lint.sh` | Discover languages, dispatch, aggregate exit codes |
| `scripts/lint-csharp.sh` | Existing SonarAnalyzer opt-in build (extend project list if needed) |
| `scripts/lint-shell.sh` | `shellcheck` on `*.sh` |
| `scripts/lint-python.sh` | `ruff check` (skip if no `*.py`) |
| `scripts/lint-cpp.sh` | `clang-tidy` (+ optional `clang-format --dry-run`) |
| `scripts/lint-make.sh` | `checkmake` on `Makefile` / `*.mk` |
| `scripts/lint/tool-versions.env` | Pinned tool versions |
| `scripts/lint/install-tools.sh` | Install missing tools into `~/.local/cb-lint/bin` |
| `scripts/lint/configs/*` | Default configs for ruff / clang-tidy / checkmake |
| `docs/LOCAL_LINT.md` | Multi-language operator doc (supersedes narrow title; keep C# Sonar section, link from LOCAL_SONAR_LINT.md) |

## Language detection

Default `./scripts/lint.sh` auto-detects and runs only present languages:

| Language | Presence heuristic |
|----------|-------------------|
| csharp | Any `*.csproj` (or `*.cs` under repo, excluding `bin`/`obj`) |
| shell | Any `*.sh` under `scripts/` (and optionally repo root) |
| python | Any `*.py` excluding `bin`/`obj`/`.venv` |
| cpp | Any `*.{c,cc,cpp,cxx,h,hpp,hxx}` or `CMakeLists.txt` |
| make | `Makefile`, `makefile`, or `*.mk` |

- Missing sources → print `skip (<lang>): no sources` and **exit 0 for that language**.
- Missing **tool** when sources exist → attempt install if `--install` was used earlier, else fail with install hint.
- Explicit args: `./scripts/lint.sh csharp shell` runs only those.

Flags:

| Flag | Meaning |
|------|---------|
| `--install` | Install/update pinned tools only; do not lint |
| `--strict` | Pass through to C# (and fail on warnings where defined) |
| `--core-only` | Pass through to C# (M1 worktrees) |
| `-h` / `--help` | Usage |

## Tooling pins (initial)

Versions live in `scripts/lint/tool-versions.env` and may be bumped deliberately:

| Tool | Role | Notes |
|------|------|-------|
| SonarAnalyzer.CSharp | Via NuGet / Directory.Build.props | Already pinned in props |
| shellcheck | Shell static analysis | Prefer distro or binary install |
| ruff | Python lint | Fast Sonar-Python stand-in |
| clang-tidy | C++ static analysis | Requires clang tools on PATH or install |
| clang-format | Optional dry-run style | Same clang package family |
| checkmake | Makefile lint | Go binary or release asset |

Install root: `$HOME/.local/cb-lint` with `bin/` on PATH for the lint scripts (`export PATH="$HOME/.local/cb-lint/bin:$PATH"`). Prefer existing PATH binaries when version is acceptable.

## Config stubs

Committed under `scripts/lint/configs/` (or repo-root copies that include/point there):

- `ruff.toml` — sensible defaults (line length, exclude venv/build)
- `.clang-tidy` — Checks enabling common bugprone/cert/modernize subset (not entire `*` to avoid noise on first backend import)
- `checkmake.ini` or `.checkmake.yml` — maxbodylength / minphony style defaults

CB-APP may have zero Python/C++/Make files; stubs still ship so Cursor agents on other checkouts find the same defaults.

## C# behavior (unchanged contract)

- Opt-in `EnableSonarAnalyzers=true` via `lint-csharp.sh`
- Severities in `.editorconfig` per SONAR_GATE.md
- Default `dotnet build` unchanged
- On tip branches that include Shell/E2E, prefer including those csproj when present (optional follow-up; Core+ChallengePass+Tests remain required)

## Docs

1. Add `docs/LOCAL_LINT.md` — multi-language entry, install, detection, limitations.
2. Keep `docs/LOCAL_SONAR_LINT.md` — either thin redirect/pointer to LOCAL_LINT.md § C#, or expand in place with a top link. Prefer **LOCAL_LINT.md as canonical** and make LOCAL_SONAR_LINT.md a short pointer to avoid drift.
3. Link from `docs/README.md` and `docs/SONAR_GATE.md` “Local verify”.
4. Mention in `AGENTS.md` under a one-liner: pre-push `./scripts/lint.sh`.

## Follow-on work queue (same program, after harness)

Ordered execution after the harness lands (user-approved):

1. **M1 / PR20** — IDE0007 burn (`var` when apparent) on Core (+ Tests as needed).
2. **M2 / PR21** — crypto review threads: A1 `PrivateKey` wipe, fused A2 production-path test, span HKDF in `DeriveAeadKey`; resolve outdated UserPrefs/HKDF threads.
3. **M2** — ChallengePass IDE0007 + coverage lift on that delta.
4. **Coverage** — drive Coverlet / Sonar new coverage toward **~90%** (exceed 80% gate).

Landing rule unchanged: fix on earliest owning stack branch, merge up.

## Success criteria

- `./scripts/lint.sh --install` installs or verifies tools without error on a clean Linux agent.
- `./scripts/lint.sh` on CB-APP tip runs **csharp + shell**, skips python/cpp/make with clear messages, exit 0 if C#/shell clean.
- Adding a single `*.py` or `Makefile` causes the corresponding linter to run (or fail loudly if tool missing).
- Docs describe the Sonar parity boundary (smells ≠ coverage/CPD).
- No change to default CI compile TreatWarningsAsErrors path without explicit opt-in.

## Spec self-review

- [x] No placeholder TBD sections left for required decisions
- [x] Scope matches Approach A (dispatcher + per-lang + user-local install)
- [x] CB-APP language inventory acknowledged (skip empty langs)
- [x] Follow-on Sonar burn queue recorded but not conflated with harness deliverable
- [x] Does not claim full Sonar QG parity
