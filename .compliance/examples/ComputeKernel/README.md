# Intent-preserving compute refactor

This example starts with a common legacy shape: a single static method allocates a full-size scratch array on every call, checks for a GPU device with no availability verification, and calls straight into an undefined external CUDA binding.

The refactor separates:

- Core: the `IReductionKernel`/`IComputeDevice` ports and a pure, dependency-free scalar reference implementation.
- Infrastructure: a pooled-buffer implementation and a device-backed implementation that falls back to an injected fallback kernel when no device is available.
- Tests: a differential test between the scalar and pooled kernels, and a fallback test for the device-backed kernel.

The example files are reference snippets and are not added to the receiving solution automatically. `AcceleratedReductionKernel` intentionally stops short of a concrete GPU library call — wire in ILGPU, ComputeSharp, or another chosen package's kernel dispatch where marked; see `../../docs/GPU-COMPUTE.md`.

## Preserved intent

- The sum is computed over every element in input order.
- A missing or unavailable device does not fail the call; it falls back to a CPU implementation.

## Deliberate improvements

- No implicit per-call scratch allocation on the pooled path; the rented buffer is always returned, including on an exceptional path.
- Device availability is checked explicitly and has a tested fallback, instead of an unverified inline branch.
- Cancellation is threaded through every implementation.
- The scalar, pooled, and device-backed kernels are interchangeable behind one port and are each independently testable.

Read files in numeric order. See `../../docs/MEMORY-COMPUTE.md` and `../../docs/GPU-COMPUTE.md` for the full method.
