# GPU compute

## Principle

A GPU is another device, in the same sense `MIGRATION-PLAYBOOK.md`'s ownership test already uses: "Does this speak SQL, HTTP, files, queues, devices, or telemetry? → Infrastructure." A GPU compute kernel is an Infrastructure adapter behind a Core-defined port, not Core itself — even when the invoked library is small and the algorithm is pure. Reach for it only after `SIMD-AND-VECTORIZATION.md`'s CPU-side options have been measured and found insufficient: host/device transfer and kernel-launch overhead mean a GPU is not automatically faster for a given workload.

Unlike SIMD/hardware intrinsics, GPU compute has no first-party BCL API. Every option below is a package-selection decision (`PACKAGE-SELECTION.md`), not a framework default.

## When to reach for it

- A profiler shows a numeric, massively data-parallel workload (thousands to millions of independent elements) dominates cost, after CPU vectorization (`SIMD-AND-VECTORIZATION.md`) has already been tried.
- The data is large enough, and reused across enough operations, to amortize the host↔device transfer cost. A single small buffer summed once is almost never worth it.
- The target deployment environment is known to have a compatible device (or the workload can correctly and silently fall back to the CPU when it does not).

If any of these does not hold, stay on the CPU path.

## Choosing a library

| Approach | Use when | Platform reach |
|---|---|---|
| ILGPU | Portable GPU compute from pure C#, with CUDA, OpenCL, and a built-in CPU accelerator for debugging/fallback | Windows, Linux, macOS; CUDA needs an NVIDIA device, OpenCL is broader |
| ComputeSharp | Compute/pixel shaders written in C#, dispatched through DirectX 12 (and D2D1), with source generators instead of runtime codegen | Windows only; includes a software (WARP) device usable as a CPU fallback |
| Direct interop (CUDA, Vulkan, raw DirectX bindings) | An existing native kernel/toolchain must be reused, or a capability neither library above exposes is required | Whatever the underlying native API supports; substantially more integration and safety work |

Prefer ILGPU or ComputeSharp over hand-rolled interop; both already solve device enumeration, buffer management, and (for ILGPU) a CPU fallback accelerator that the other options leave to you. Record the choice in `PACKAGE-SELECTION.md` — trimming/AOT compatibility and transitive native-dependency size are both real considerations here.

## Architectural placement

- Define the port in Core: an interface shaped around what the use case needs (`SumAsync`, `TransformAsync`), not around the chosen library's API surface. This mirrors `ISpeciesCatalog`/`IMeasurementStore` in `examples/MeasurementSlice/`.
- Keep a pure, dependency-free CPU reference implementation of that port in Core — the same "keep the scalar implementation in the codebase, not only in version history" rule `SIMD-AND-VECTORIZATION.md` states.
- Implement the GPU-backed adapter in Infrastructure. It owns device selection, buffer allocation/transfer, kernel dispatch, and disposal of any device-side resources.
- Check device availability explicitly (an `IsAvailable`-shaped check) before dispatching, and fall back to the CPU implementation — composed in, not duplicated — when no compatible device is present or initialization fails. See `examples/ComputeKernel/` for the shape.

## Memory and transfer

- Host↔device transfer is not free; batch it. Transferring a buffer once and running several kernel passes on the device beats transferring once per pass.
- Pin host memory that will be transferred repeatedly (see `MEMORY-COMPUTE.md`); an unpinned managed array can be moved by the GC mid-transfer on some interop paths.
- Dispose device buffers deterministically. A GPU-side leak does not show up in a managed heap profiler and will exhaust device memory silently until a kernel launch fails.
- Do not assume the device and host share memory; measure whether a workload's transfer cost exceeds the compute it saves before committing to the device path.

## Correctness and numeric behavior

- GPU floating-point arithmetic can differ from the CPU's: different reduction/summation order, different fused-multiply-add availability, and different rounding for some transcendental functions. This is an approved, documented behavior difference — the same discipline `INTENT-TRANSLATION.md` requires for any change in observable output — not a bug to silently chase to bit-for-bit equality.
- Differentially test the GPU-backed implementation against the CPU reference across representative and boundary inputs, with an explicitly stated tolerance (ULP or absolute/relative), the same method `SIMD-AND-VECTORIZATION.md` and `TESTING-PLAYBOOK.md` already require for vectorized code.
- Test the fallback path itself: force "device unavailable" and confirm the CPU implementation runs and produces the same class of result, not just that no exception is thrown.

## Benchmarking and operations

- Benchmark end-to-end, including transfer, not just kernel execution time; a kernel that is fast on-device can still lose to the CPU once transfer is counted.
- Benchmark on the device class actually deployed; a workstation GPU is not a proxy for a CI runner or a production container with no GPU at all.
- Document the runtime/driver dependency (CUDA toolkit version, DirectX feature level) the same way any other external dependency is documented, and confirm the CPU fallback is exercised in any environment that cannot guarantee the device is present, including CI.
- Native AOT and trimming: verify the chosen library's compatibility before enabling either (`FEATURE-ADOPTION.md` Stage 7); GPU libraries commonly ship native binaries or use runtime codegen that need explicit support.

## Related

- `SIMD-AND-VECTORIZATION.md` for the CPU-side options to exhaust first.
- `MEMORY-COMPUTE.md` for pinning and buffer-ownership discipline that host↔device transfer depends on.
- `LEGACY-PATTERN-MAP.md` for the CPU-kernel-to-GPU-adapter migration row.
- `TESTING-PLAYBOOK.md` for the differential and fallback testing method.
- `COMPLIANCE-CHECKLIST.md` for the GPU evidence items.
- `examples/ComputeKernel/` for a worked port/fallback example.
- The scanner's `GPU001` finding flags a GPU-library entry point with no visible `try`/`catch` and no fallback marker in the same file; it cannot see a fallback implemented elsewhere, so treat it as a prompt to verify, not proof of a defect.
