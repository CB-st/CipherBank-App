# SIMD and vectorization

## Principle

Vectorization is a proven optimization applied to a measured hot path, not a default coding style. Follow `FEATURE-ADOPTION.md` Stage 7: profile first, keep a correct scalar reference, and prove equivalence before trusting the accelerated path.

## When to reach for it

- A profiler — not intuition — shows a numeric, data-parallel loop dominates wall-clock or CPU time.
- The data is contiguous and large enough to amortize setup cost. "Large enough" is workload-specific; measure it, do not assume a threshold.
- The operation applies the same arithmetic independently across elements, or uses a cheap comparison mask/select with pure alternatives. Stateful or expensive per-element branching is not a SIMD candidate.

If none of these hold, keep the scalar implementation. SIMD adds real complexity and portability cost that only pays for itself on a proven hot path.

## Choosing a level of abstraction

| Approach | Use when | Cost |
|---|---|---|
| `System.Numerics.Tensors` (`TensorPrimitives`) | A common numeric reduction/elementwise operation (sum, dot product, distance, cosine similarity, activation-style functions) over a `Span<T>` | Lowest effort; already vectorized and hardware-dispatched internally, with an automatic non-accelerated fallback |
| `System.Numerics.Vector<T>` | Portable elementwise math with no need to pick an instruction set | Runtime-sized to the widest available vector; simplest hand-written vector code |
| `Vector128<T>` / `Vector256<T>` / `Vector512<T>` | Fixed-width control is needed because the algorithm's shape depends on lane count | More code to write; still portable across x86 and Arm, with a managed fallback for the cross-platform static members |
| `System.Runtime.Intrinsics.X86` / `.Arm` hardware intrinsics | A specific instruction is required and the portable APIs above do not expose it | Platform-specific; requires an explicit `IsSupported` guard and fallback — see below |

Prefer the top of this table and move down a row only when a benchmark shows it is necessary.

## Feature detection and fallback

- Every hardware-intrinsics call (`System.Runtime.Intrinsics.X86.*`, `System.Runtime.Intrinsics.Arm.*` — for example `Sse2`, `Avx2`, `Avx512F`, `AdvSimd`) must be guarded by its matching `IsSupported` check and paired with a correct scalar or `Vector<T>`/`TensorPrimitives` fallback for hardware where it is `false`. These classes throw `PlatformNotSupportedException` if called on hardware that lacks the instruction set; there is no automatic fallback the way there is for the cross-platform vector APIs.
- `Vector<T>`, the cross-platform `Vector128`/`Vector256`/`Vector512` static helper members, and `TensorPrimitives` select an appropriate width/instruction set at runtime and fall back to a non-accelerated software implementation automatically. They still benefit from `Vector.IsHardwareAccelerated` (or the width-specific `Vector128.IsHardwareAccelerated`, `Vector256.IsHardwareAccelerated`, `Vector512.IsHardwareAccelerated`) being checked where the calling code makes its own algorithmic choice based on acceleration, and from a scalar path being present for correctness testing.
- These `IsSupported`/`IsHardwareAccelerated` checks are folded to compile-time constants by the JIT, so the branch not taken is eliminated; guarding an intrinsics call is not a runtime cost worth avoiding.
- Do not gate on the operating system or process bitness as a proxy for instruction-set support; check the specific class.

## Memory layout and safety

- Vectorized code operates on contiguous memory: arrays, `Span<T>`/`ReadOnlySpan<T>`, or pinned/native memory. Convert from other shapes explicitly at the boundary of the vectorized routine.
- Keep `MemoryMarshal`/`Unsafe` usage inside the narrow vectorized routine; do not let an unsafe cast leak into calling code.
- Handle the remainder when the element count is not a multiple of the vector width; test the boundary explicitly (0, 1, width − 1, width, width + 1 elements).
- Watch alignment and false sharing when vectorized code runs across threads over shared buffers; partition ranges so threads do not write adjacent cache lines.

## Correctness and numeric behavior

- A vectorized reduction can change floating-point rounding relative to a naive left-to-right scalar loop because the summation order changes. This is expected, not a bug — but it is an approved, documented behavior difference, the same discipline `INTENT-TRANSLATION.md` requires for any change in observable output.
- Prove the vectorized path against the scalar reference with differential testing over representative and boundary inputs, including `NaN`, `Infinity`, negative zero, and denormals. State the comparison tolerance explicitly (exact bits, ULP, or an absolute/relative tolerance) rather than defaulting to bit-for-bit equality.
- Keep the scalar implementation in the codebase as the reference and fallback, not only in version history.

## Benchmarking

- Measure with BenchmarkDotNet (see `PACKAGE-SELECTION.md`), not a `Stopwatch` in a loop; report allocation alongside throughput.
- Benchmark on the hardware class the deployment target actually uses. An instruction set available in CI or on a developer workstation is not guaranteed in production.
- Re-benchmark after a .NET SDK or hardware change that could shift auto-vectorization, `TensorPrimitives` dispatch, or the JIT's own codegen.

## Where this code lives

- A vectorized routine that is a pure, deterministic function of its inputs belongs in Core alongside its scalar counterpart. Hardware intrinsics and `System.Numerics` types are part of the base class library, not an external SDK, so using them does not violate the Core boundary in `AGENTS.core.md`.
- Keep the `IsSupported` branch and the vectorized/scalar selection inside that Core routine; do not let platform-conditional control flow leak into Application or Host code.
- Native AOT and trimming: hardware-intrinsics and `Vector<T>`/`TensorPrimitives` code is AOT- and trimming-compatible. Reflection-based numeric libraries may not be. Re-verify after enabling Native AOT (`FEATURE-ADOPTION.md` Stage 7).

## Related

- `FEATURE-ADOPTION.md`, Stage 7, for sequencing this against other optimization decisions.
- `LEGACY-PATTERN-MAP.md` for the manual-loop migration row.
- `TESTING-PLAYBOOK.md` for the differential and property-based testing method.
- `COMPLIANCE-CHECKLIST.md` for the SIMD evidence items.
- `MEMORY-COMPUTE.md` for the buffer layout and pooling that vectorized code most often runs over.
- `GPU-COMPUTE.md` for the next step once CPU-side vectorization is exhausted.
- `BRANCHLESS-PROGRAMMING.md` for masked selection, branch-distribution benchmarks, generated-code evidence, and constant-time distinctions.
- The scanner's `SIMD001` finding flags a hardware-intrinsics member used without a nearby matching intrinsic-type `IsSupported` guard; treat it as a prompt for control-flow review, not proof that no valid helper guard exists.
