# Local multi-language lint harness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `./scripts/lint.sh` that runs Sonar-aligned C# lint plus portable shell/Python/C++/Make linters with skip-if-no-sources and user-local tool install.

**Architecture:** Thin bash dispatcher discovers languages, prepends `~/.local/cb-lint/bin` to PATH, and calls per-language scripts. Config stubs and pinned versions live under `scripts/lint/`. C# keeps existing `lint-csharp.sh` + opt-in SonarAnalyzer.

**Tech Stack:** bash, shellcheck, ruff, clang-tidy/clang-format, checkmake, existing SonarAnalyzer.CSharp NuGet

**Spec:** [docs/superpowers/specs/2026-07-30-local-multi-lang-lint-design.md](../specs/2026-07-30-local-multi-lang-lint-design.md)

## Global Constraints

- Opt-in only for SonarAnalyzer (`EnableSonarAnalyzers`); do not change default `dotnet build` WAE behavior.
- Skip languages with no sources (exit 0 for that language).
- Install tools to `$HOME/.local/cb-lint` — never commit binaries.
- Do not claim coverage/CPD parity with Sonar QG.
- Follow AGENTS.md function-doc conventions only for C#; bash scripts get header comments.
- Commit author for this repo: `git -c user.name=skyrailmaxima -c user.email=skyrailmaxima@gmail.com` when committing (no `git config` writes).

---

### Task 1: Tool versions + install helper + configs

**Files:**
- Create: `scripts/lint/tool-versions.env`
- Create: `scripts/lint/install-tools.sh`
- Create: `scripts/lint/configs/ruff.toml`
- Create: `scripts/lint/configs/clang-tidy.yaml` (or `.clang-tidy` content file)
- Create: `scripts/lint/configs/checkmake.ini`
- Create: `scripts/lint/lib.sh` (shared: repo root, PATH prepend, find helpers)

**Interfaces:**
- Produces: `cb_lint_root`, `cb_lint_ensure_path`, `cb_lint_has_sources` helpers; pinned env vars `SHELLCHECK_VERSION`, `RUFF_VERSION`, etc.

- [ ] **Step 1:** Add `tool-versions.env` with pinned versions (document rationale in comments).
- [ ] **Step 2:** Add `lib.sh` with ROOT detection, PATH prepend for `~/.local/cb-lint/bin`, and file-find helpers excluding `bin/`, `obj/`, `.git/`, `.venv/`.
- [ ] **Step 3:** Add `install-tools.sh` that installs missing shellcheck/ruff/checkmake (and notes clang-tidy must come from apt/llvm if absent); idempotent; `--help`.
- [ ] **Step 4:** Add config stubs under `scripts/lint/configs/`.
- [ ] **Step 5:** Run `./scripts/lint/install-tools.sh` and verify binaries exist or clear skip messages for clang.

---

### Task 2: Per-language lint scripts

**Files:**
- Create: `scripts/lint-shell.sh`
- Create: `scripts/lint-python.sh`
- Create: `scripts/lint-cpp.sh`
- Create: `scripts/lint-make.sh`
- Modify: `scripts/lint-csharp.sh` (source lib PATH; optional include Shell/E2E csproj when present)

**Interfaces:**
- Consumes: `lib.sh`, `tool-versions.env`
- Produces: each script exits 0 on skip/success, non-zero on lint failure; prints `skip (<lang>): …` when no sources

- [ ] **Step 1:** Implement `lint-shell.sh` — shellcheck all `scripts/**/*.sh` (and root `*.sh` if any).
- [ ] **Step 2:** Implement `lint-python.sh` — `ruff check` with `--config scripts/lint/configs/ruff.toml` or skip.
- [ ] **Step 3:** Implement `lint-cpp.sh` — clang-tidy on discovered sources or skip; fail with install hint if sources exist but tool missing.
- [ ] **Step 4:** Implement `lint-make.sh` — checkmake on Makefiles or skip.
- [ ] **Step 5:** Smoke: run each script on CB-APP tip (expect shell run; python/cpp/make skip).

---

### Task 3: Dispatcher + docs

**Files:**
- Create: `scripts/lint.sh`
- Create: `docs/LOCAL_LINT.md`
- Modify: `docs/LOCAL_SONAR_LINT.md` (pointer to LOCAL_LINT.md)
- Modify: `docs/README.md`, `docs/SONAR_GATE.md`, `AGENTS.md` (one-liner)

**Interfaces:**
- Consumes: all `lint-*.sh`, `install-tools.sh`
- Produces: `./scripts/lint.sh` aggregate exit code

- [ ] **Step 1:** Implement `lint.sh` with auto-detect, `--install`, `--strict`, `--core-only`, language filters.
- [ ] **Step 2:** Write `docs/LOCAL_LINT.md`; shrink `LOCAL_SONAR_LINT.md` to pointer + C# deep link.
- [ ] **Step 3:** Wire README / SONAR_GATE / AGENTS references.
- [ ] **Step 4:** Run `./scripts/lint.sh` on tip; fix shellcheck findings in our new scripts if any.
- [ ] **Step 5:** Commit harness on current branch (`prototype/maui-m4` or as directed) with message focused on why (portable pre-push lint).

---

### Task 4: Hand-off to Sonar burn queue (separate commits)

Not part of the harness PR body beyond a doc mention; execute next:

1. Checkout `prototype/maui-m1` — IDE0007 burn
2. Checkout `prototype/maui-m2` — A1 wipe, fused A2 test, span HKDF; ChallengePass IDE0007 + coverage
3. Coverage push toward ~90%
4. Merge up stack

---

## Plan self-check

- [x] Spec referenced
- [x] Files named exactly
- [x] Skip-empty and install model covered
- [x] Follow-on burn listed separately so harness stays reviewable alone
