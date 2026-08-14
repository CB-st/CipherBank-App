# Branchless selection example

This example separates four concerns that are often incorrectly collapsed into “branchless code”:

- `00-Reference.cs` is the readable behavioral oracle.
- `01-ScalarSemantics.cs` uses framework operations and leaves code generation to the JIT.
- `02-Vectorized.cs` uses portable SIMD comparison masks and `Vector.ConditionalSelect`, with scalar dispatch and tail fallback.
- `03-FixedTimeSecurity.cs` uses the platform constant-time comparison API rather than a hand-written bit trick.
- `04-Tests.cs` covers vector-width boundaries, floating-point special values, length validation, and fixed-length token behavior.
- `05-Benchmarks.cs` compares branchy and masked implementations on random, skewed, and sorted inputs.

## Adoption workflow

1. Copy the reference and candidate into an actual .NET 10 test/benchmark project.
2. Add `xunit` to the test project and `BenchmarkDotNet` only to a dedicated benchmark project through the package-selection process.
3. Add a production corpus/distribution; the synthetic datasets are not sufficient evidence.
4. Run Release benchmarks without a debugger. Inspect the disassembly after warmup.
5. Add hardware branch counters when the host supports reliable collection.
6. Repeat on every supported architecture, especially x64 and Arm64.
7. Complete `templates/BRANCHLESS-PERFORMANCE-RECORD.md` and retain the vectorized implementation only if it materially improves the real workload.

The benchmark source is guidance and is not compiled or installed into the receiving solution automatically. API availability and generated instructions must be verified with the receiving repository's pinned .NET 10 SDK/runtime.
