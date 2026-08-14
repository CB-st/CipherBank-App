# Compute in memory

## Principle

Reduce allocation and copying on a measured hot path, not by default. The same discipline as `SIMD-AND-VECTORIZATION.md` applies: profile first, prove correctness, and keep an owner responsible for every buffer's lifetime. Memory optimization that trades a GC allocation for an unreleased native block or a un-returned pooled array is not a win.

## Choosing a level of abstraction

| Approach | Use when | Cost |
|---|---|---|
| `Span<T>`/`ReadOnlySpan<T>` over an existing array/string | Slicing or reading contiguous data without copying | Lowest effort; stack-only, cannot be stored in a field of a type that outlives the call unless it is a `ref struct` used carefully |
| `ArrayPool<T>.Shared` | Short-lived buffers requested/released frequently on a hot path | Simple `Rent`/`Return` pairing; must be released on every exit path including exceptions |
| `MemoryPool<T>.Shared` | The same need, but the buffer's lifetime should be owned by an `IDisposable` | Slightly more overhead than `ArrayPool<T>` directly; ownership is explicit via `IMemoryOwner<T>` |
| `stackalloc` / `Span<T>` over stack memory | A small, bounded, compile-time-or-provably-small allocation that does not escape the current call | No GC or pooling involved; unbounded or data-dependent sizes risk a stack overflow |
| `NativeMemory`/`Marshal` unmanaged allocation | Memory must outlive the call, be pinned for interop, or exceed what the stack/pool should hold | Full manual lifetime management; nothing frees it for you |
| Memory-mapped file (`MemoryMappedFile`) | Data larger than should be loaded into the managed heap at once, or shared across processes | OS-managed paging; still needs an explicit `Dispose` on the view and the map |

Prefer the top of this table and move down a row only when a benchmark shows it is necessary, exactly as `SIMD-AND-VECTORIZATION.md` recommends for vectorization.

## Ownership and disposal

- Every pooled or native allocation has exactly one owner responsible for releasing it. Wrap it in an `IDisposable` type, or release it in a `finally` block, so every exit path — including an exception — releases it.
- `ArrayPool<T>.Rent`/`Return` and `NativeMemory.Alloc`/`Free` do not throw if you forget to release; they leak silently. Treat a missing release the same as any other resource leak the checklist already asks about (`AGENTS.infrastructure.md`, `COMPLIANCE-CHECKLIST.md`).
- `MemoryPool<T>.Rent()` returns an `IMemoryOwner<T>`; releasing it is `Dispose()`, not `Return()`. Do not mix the two idioms on the same buffer.
- Do not use a rented or pooled buffer after returning/disposing it, and do not return a `Span<T>`/`stackalloc` span from the method that created it.
- Clear a returned buffer (`Return(array, clearArray: true)`, or `AllocZeroed` on allocation) when it may contain sensitive data; the pool does not clear it for you by default.

## Safety

- `stackalloc` size must be small and bounded — a compile-time constant or a value with an enforced upper bound. An attacker- or data-controlled size risks a stack overflow with no recoverable exception.
- Keep `Span<T>`, `stackalloc`, and `Unsafe`/`MemoryMarshal` usage inside a narrow routine; do not let a `ref struct` or raw pointer leak into a wider API surface than it needs to.
- Native memory obtained from `NativeMemory`/`Marshal` is not tracked or scanned by the GC and is not zeroed unless you asked for that explicitly (`AllocZeroed`); do not assume either guarantee.
- Concurrent access to shared pooled or native memory needs the same synchronization discipline as any other shared mutable state; a rented buffer handed to two operations at once is a data race, not a saved allocation.

## Testing

- Prove a pooled or native-backed implementation returns the same result as a plain managed-array implementation across representative and boundary inputs, the same differential-testing discipline `SIMD-AND-VECTORIZATION.md` and `TESTING-PLAYBOOK.md` already require for vectorized code.
- Test the release path explicitly: a fake/counting pool (or a debug allocator) that fails the test if `Rent` outpaces `Return`, or if `Free` is never observed after `Alloc`, catches a leak that a correctness-only test will not.
- Benchmark with BenchmarkDotNet's memory diagnoser (`PACKAGE-SELECTION.md`) rather than assuming pooling is faster; for small buffers, allocating a new managed array is sometimes cheaper than renting one.

## Related

- `SIMD-AND-VECTORIZATION.md` for the vectorized code that most often consumes these buffers.
- `GPU-COMPUTE.md` for host-to-device transfer, which builds directly on the pinning/native-memory guidance above.
- `LEGACY-PATTERN-MAP.md` for the per-call allocation migration row.
- `COMPLIANCE-CHECKLIST.md` for the memory-ownership evidence items.
- The scanner's `MEM001` finding flags a native allocation with no matching free call anywhere in the same file; `MEM002` flags an `ArrayPool<T>` rental with no matching return. Treat both as prompts to verify the release exists, not as proof it is missing.
