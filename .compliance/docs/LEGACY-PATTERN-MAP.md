# Legacy-to-modern pattern map

| Original shape | Target shape | Preserve before moving | Best proof |
|---|---|---|---|
| Controller/view model contains calculations and EF calls | Thin boundary + Core policy + repository port | validation order, rounding, transaction, status/UI state | characterization + policy unit + database integration + host test |
| `new HttpClient()` per operation | typed client from `IHttpClientFactory` | base URI, headers, timeout, serialization, error handling | stub-server contract + transient sequence test |
| `IConfiguration` injected everywhere | typed validated options | key names, defaults, environment overrides, missing-value behavior | startup/options tests |
| `DateTime.UtcNow` in policy | injected `TimeProvider` | UTC/local interpretation and capture instant | fixed-clock boundary tests |
| `.Result` or `.Wait()` | async end-to-end | ordering, exception type, caller lifetime | async behavior + cancellation + shutdown tests |
| `async void` command/service | `Task`-returning operation; thin UI event wrapper | UI error and busy-state behavior | view-model/service unit + UI event integration |
| static mutable service locator | constructor-injected ports | instance lifetime and shared-state behavior | DI composition + concurrency tests |
| EF entity used as API/domain model | separate entity, domain value, DTO | field names, null/default behavior, concurrency token | mapping + contract + provider integration tests |
| `EnsureCreated` on startup | reviewed migrations | deployed schema and seed behavior | upgrade rehearsal from previous schema |
| interpolated log messages | structured message templates | level, operation name, correlation needs | capturing-provider test; avoid exact prose assertions |
| unbounded queue | bounded `Channel<T>` | overflow behavior, ordering, producer blocking/drop policy | backpressure + cancellation + shutdown tests |
| broad catch returning false/null | expected result + exceptional infrastructure faults | which failures callers distinguish and retry | failure-partition unit and integration tests |
| magic numeric units | strong quantity or explicit canonical unit | unit conversion, significant figures, tolerance | reference dataset + round-trip/property tests |
| reflection-heavy mapping | explicit or source-generated mapping | ignored/default/renamed/null members | mapping tests + trimming/AOT build where relevant |
| hand-rolled `INotifyPropertyChanged` boilerplate wired through code-behind | thin view + source-generated observable view model calling Application | binding property names, change-notification timing, command enablement | view-model unit tests asserting notifications and `CanExecute` |
| hand-written scalar loop identified as a numeric hot path | `TensorPrimitives`/`Vector<T>` or guarded hardware intrinsics with a scalar fallback | exact output only where required; otherwise document the approved rounding/order change | differential test against the scalar reference with a stated tolerance + benchmark comparison |
| ad hoc `byte[]`/large-object allocation per call in a hot path | pooled `ArrayPool<T>`/`MemoryPool<T>` rental with one explicit owner and disposal | buffer size, clearing/zeroing expectations, thread-affinity | allocation benchmark + rent/return or alloc/free leak test |
| CPU-only numeric kernel identified as a GPU-suitable hot path | Infrastructure GPU kernel behind a Core-defined port, with a device-availability check and CPU fallback | exact output only where required; otherwise document the approved precision/order change | differential test against the CPU reference with a stated tolerance + device-availability/fallback test |
| deeply nested function mixing validation, policy, and I/O | guard clauses + named pure decisions + short coordinator + explicit outcome | branch precedence, failure meaning, side effects, and ordering | characterization decision table + branch/boundary tests + Sonar S3776 |
| large object using `IServiceProvider` and unrelated dependencies | cohesive invariant-owning object + narrow constructor-injected ports + outer composition | service lifetime, transaction boundary, and externally visible sequence | DI scope validation + contract/integration tests |
| concatenated SQL/command or unchecked path/URI from input | parameterized API + allowlist/canonicalization + bounded operation | accepted valid inputs and exact rejection policy | positive cases + negative injection/traversal/SSRF cases + Hotspot review |
| unpredictable conditional inside a profiled numeric hot loop | scalar oracle + semantic API or portable SIMD comparison mask/conditional select | numeric, exception, tail, aliasing, and side-effect semantics | differential/property tests + distribution-aware benchmark + disassembly |
| hand-written “constant-time” XOR/mask loop | reviewed platform cryptographic primitive such as `CryptographicOperations.FixedTimeEquals` | protocol length policy and comparison result | known vectors + malformed-length tests + security review; timing alone is not proof |

The target shape is a direction, not a mandate. If the original shape is already small, deterministic, and contained, adding layers may make it worse.
