# Installed compliance workspace

The `.compliance/` directory is owned by the overlay. Application code remains in its existing locations while it is migrated by tested vertical slices.

## Documents

1. `MIGRATION-PLAYBOOK.md`: staged repository migration.
2. `INTENT-TRANSLATION.md`: method for converting original behavior into explicit contracts.
3. `TESTING-PLAYBOOK.md`: characterization through final verification.
4. `FEATURE-ADOPTION.md`: target feature and package adoption sequence.
5. `PACKAGE-SELECTION.md`: dependency decision record template.
6. `AGENT-PLACEMENT.md`: placing scoped instructions in an existing layout.
7. `COMPLIANCE-CHECKLIST.md`: final implementation and evidence gate.
8. `LEGACY-PATTERN-MAP.md`: old shapes, target patterns, and proof methods.
9. `UI-COMPOSITION.md`: view/view-model ownership, dispatcher boundaries, and accessibility.
10. `SIMD-AND-VECTORIZATION.md`: when to vectorize, feature detection/fallback, and correctness.
11. `MEMORY-COMPUTE.md`: pooled/native/mapped memory, ownership, disposal, and safety.
12. `GPU-COMPUTE.md`: when to use a GPU, library choice, device fallback, and correctness.
13. `BRANCHLESS-PROGRAMMING.md`: measured branch-removal, masked selection, constant-time distinctions, benchmarking, and acceptance evidence.
14. `SONARQUBE-DEVELOPMENT-STANDARD.md`: function/object construction, method-level complexity, security, and suppression rules.
15. `SONARQUBE-SETUP.md`: server profile/gate decisions and the .NET begin/build/test/end workflow.

## Templates

Copy and adapt the nearest `templates/AGENTS.*.md` file into the matching source subtree. Existing agent instructions are deliberately preserved and require manual reconciliation. Dedicated `AGENTS.ui.md`, `AGENTS.sonar.md`, and `AGENTS.branchless.md` templates cover UI ownership, analyzer governance, and measured hot-path optimization. Use `BRANCHLESS-PERFORMANCE-RECORD.md` for each retained branchless change.

## Examples

- `examples/MeasurementSlice/` shows one behavior split into Core policy, Application orchestration, Infrastructure persistence, API mapping, and tests.
- `examples/UiCommand/` shows the same Application layer reused from a thin, source-generated UI view model, refactored from a blocking code-behind handler.
- `examples/ComputeKernel/` shows a scalar Core reference implementation reused behind pooled and device-backed Infrastructure kernels, with a tested CPU fallback.
- `examples/CognitiveComplexity/` shows a behavior-preserving decision-table refactor from nested branching to guards, named policy, typed outcomes, and branch tests.
- `examples/BranchlessSelection/` separates a scalar oracle, semantic scalar APIs, portable SIMD mask/select, fixed-time comparison, differential tests, and distribution-aware benchmarks.
