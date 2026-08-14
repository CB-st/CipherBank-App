# Branchless optimization contract

This scope contains measured performance-critical code where branch elimination, masked selection, or SIMD may be considered.

## Rules

- Read `.compliance/docs/BRANCHLESS-PROGRAMMING.md` and `.compliance/docs/SIMD-AND-VECTORIZATION.md` before changing a hot loop.
- Do not optimize an unprofiled method or apply branchless style to business rules, validation, authorization, I/O, or orchestration.
- Preserve a readable scalar reference and establish differential tests before implementation.
- Prefer semantic framework operations and portable `Vector<T>` masks before manual bit tricks or platform intrinsics.
- Never claim source code is branchless. Inspect warmed shipping-configuration disassembly.
- Do not equate branchless code with constant-time security. Use approved cryptographic APIs and request security review.
- Keep both alternatives pure and safe for eager evaluation; preserve overflow, floating-point, exception, ordering, and side-effect semantics.

## Required evidence

- Complete `.compliance/templates/BRANCHLESS-PERFORMANCE-RECORD.md`.
- Benchmark realistic sizes and predictable, skewed, random, sorted, and production distributions.
- Record runtime, CPU/architecture, instruction sets, PGO/AOT state, disassembly, noise, and end-to-end effect.
- Test lengths around vector width, numeric extremes, `NaN`/infinities/signed zero where applicable, and randomized differential corpora.
- Retain the candidate only when the measured gain justifies its maintenance and portability cost.
