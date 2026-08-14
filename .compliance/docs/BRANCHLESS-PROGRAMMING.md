# Branchless programming standard

Branchless programming is a targeted performance technique, not a repository-wide style rule. Use it only in measured hot loops or for a separately reviewed constant-time security requirement. Ordinary branches are clearer and often faster when they are predictable, when one arm is expensive, or when only one arm may execute.

Source code that looks branchless does not prove branchless machine code. The .NET JIT can turn a ternary, `Math.Clamp`, vector mask, or ordinary `if` into branches, conditional moves, predication, or different code after tiered compilation and dynamic PGO. Generated disassembly and measurements on supported target architectures are the evidence.

## Decision standard

| Situation | Default | Branchless candidate? |
|---|---|---|
| Business rules, validation, authorization, orchestration | readable branches and guard clauses | no |
| Predictable hot/cold condition | keep the branch; let PGO lay out hot code | rarely |
| Unpredictable per-element condition in a measured hot loop | retain a branchy reference | yes, if both outcomes are cheap and pure |
| One outcome performs I/O, allocates, throws, logs, locks, or mutates state | execute only the selected outcome | no |
| Bulk numeric selection/transformation | semantic `Math` API, then `Vector<T>` masks | often |
| Platform-specific intrinsic path | guarded dispatch plus scalar/vector fallback | only after portable SIMD |
| Secret-dependent comparison/control flow | approved cryptographic API and security review | separate constant-time requirement |

Do not create a branchless implementation merely because a condition exists. A correctly predicted branch can be cheaper than computing both alternatives and blending them. Branch removal can also increase instruction count, register pressure, memory traffic, power use, and cognitive complexity.

## Required implementation order

1. **Measure the real workload.** Identify the hot method and quantify its contribution to end-to-end cost.
2. **Freeze behavior.** Keep a readable scalar reference and add differential tests before optimization.
3. **Describe the branch distribution.** Record representative ratios such as 50/50 unpredictable, 99/1 skewed, sorted, bursty, or correlated.
4. **Prefer semantic APIs.** Try `Math.Clamp`, `Math.Min`/`Max`, spans, batching, and `System.Numerics.Vector<T>` before manual bit manipulation or ISA-specific intrinsics.
5. **Make both alternatives safe to evaluate.** A mask/select implementation may compute both sides. Neither side may throw, perform I/O, mutate state, allocate materially, or depend on evaluation order.
6. **Preserve numeric semantics.** State overflow mode, rounding, `NaN`, infinities, signed zero, and conversion behavior. Bitwise integer tricks do not automatically preserve floating-point behavior.
7. **Implement masked selection.** For bulk data, derive a comparison mask and use `Vector.ConditionalSelect` or the corresponding `Vector128/256/512.ConditionalSelect` API.
8. **Handle the tail explicitly.** The remainder loop may contain branches; correctness and bounds safety matter more than eliminating a predictable loop exit.
9. **Benchmark every candidate.** Compare reference and candidate across realistic distributions, sizes, warmup/tiered states, and each supported CPU architecture.
10. **Inspect generated code.** Confirm what the JIT emitted. Record runtime, architecture, instruction set, PGO/AOT mode, and disassembly evidence.
11. **Keep or revert.** Retain the optimization only when the measured end-to-end benefit exceeds its complexity and portability cost.

## C# implementation rules

### Scalar code

- Write the clearest scalar expression first. A ternary is not a guarantee of a conditional move.
- Prefer `Math.Clamp`, `Math.Min`, `Math.Max`, `Math.Abs`, and other semantic framework operations to sign-bit tricks.
- Do not convert `bool` to an integer mask through unsafe layout assumptions.
- Do not use `value & ~(value >> 31)`-style tricks without exhaustive boundary proof, disassembly, and a documented benefit. They are overflow/type-width traps and often obscure intent for no gain.
- Preserve `checked`/`unchecked` behavior deliberately.

### SIMD masked selection

```csharp
var values = new Vector<float>(inputSlice);
var mask = Vector.GreaterThanOrEqual(values, threshold);
var selected = Vector.ConditionalSelect(mask, highValue, lowValue);
selected.CopyTo(outputSlice);
```

Comparison produces an all-bits-set/all-bits-clear lane mask. `ConditionalSelect` selects on a bitwise basis. Use matching element/mask types; do not reinterpret arbitrary masks unless the bit layout is proven.

Use `Vector<T>` for portable, runtime-width SIMD. Use `Vector128<T>`, `Vector256<T>`, or `Vector512<T>` only when a fixed width is part of the measured design. Platform-specific X86/Arm classes still require the matching `IsSupported` guard and fallback described in `SIMD-AND-VECTORIZATION.md`.

### Constant-time security

Branchless and constant-time are not synonyms. Constant-time behavior can be affected by JIT transformations, memory access, table lookups, cache behavior, operand-dependent instructions, exceptions, and length checks.

- Use `CryptographicOperations.FixedTimeEquals` for supported byte-sequence equality instead of a hand-written XOR loop.
- Treat input length as public or enforce a fixed protocol length before comparison. Document that decision.
- Use platform cryptographic primitives for authentication, MACs, signatures, and key operations.
- Require security review and protocol-level negative tests. A microbenchmark with similar timings is not proof of constant-time behavior.

## Semantic hazards checklist

- [ ] Only the selected branch previously executed; computing both alternatives is proven safe.
- [ ] Exception timing/type and validation order are unchanged or intentionally revised.
- [ ] Integer overflow, shifts, signedness, and conversion widths are explicit.
- [ ] Floating-point `NaN`, infinities, negative zero, denormals, and rounding are covered.
- [ ] Aliasing/overlapping spans are supported explicitly or rejected.
- [ ] Bounds checks and tail handling are correct for lengths `0`, `1`, `width - 1`, `width`, and `width + 1`.
- [ ] No logging, metrics, volatile access, lock, allocation, or I/O is duplicated by eager evaluation.
- [ ] Public results, ordering, and side effects match the reference implementation.

## Performance evidence standard

Use BenchmarkDotNet or an equivalent controlled harness. At minimum record:

- benchmark commit and command;
- .NET runtime/SDK, tiered compilation, dynamic PGO, ReadyToRun/AOT state;
- OS, CPU model, architecture, and available instruction sets;
- input sizes and branch distributions;
- mean/median, error/noise, allocation, and throughput;
- disassembly for the hot method;
- branch instructions/mispredictions when reliable hardware counters are available;
- end-to-end confirmation outside the microbenchmark.

Test at least predictable/skewed, 50/50 unpredictable, sorted, and production-representative data. A branchless candidate that wins only on artificial random input does not justify replacing production code.

## Acceptance gate

A branchless change is complete only when:

- [ ] profiling identifies a material hot path;
- [ ] the scalar reference remains available in tests or production fallback;
- [ ] differential/property tests cover the full semantic boundary;
- [ ] benchmark inputs represent production distributions and sizes;
- [ ] the candidate wins on every required target or dispatches/falls back safely;
- [ ] generated code was inspected after warmup for the shipping configuration;
- [ ] the gain and complexity tradeoff are recorded in `templates/BRANCHLESS-PERFORMANCE-RECORD.md`;
- [ ] SonarQube and normal correctness/security gates still pass;
- [ ] reviewers can remove the optimization later using the recorded reference and tests.

## Official references

- .NET SIMD guidance: <https://learn.microsoft.com/dotnet/standard/simd>
- `Vector.ConditionalSelect`: <https://learn.microsoft.com/dotnet/api/system.numerics.vector.conditionalselect?view=net-10.0>
- `Vector128.ConditionalSelect`: <https://learn.microsoft.com/dotnet/api/system.runtime.intrinsics.vector128.conditionalselect?view=net-10.0>
- `Vector.IsHardwareAccelerated`: <https://learn.microsoft.com/dotnet/api/system.numerics.vector.ishardwareaccelerated?view=net-10.0>
- `CryptographicOperations.FixedTimeEquals`: <https://learn.microsoft.com/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals?view=net-10.0>
- .NET compilation and dynamic PGO settings: <https://learn.microsoft.com/dotnet/core/runtime-config/compilation>
- BenchmarkDotNet diagnosers: <https://benchmarkdotnet.org/articles/configs/diagnosers.html>
