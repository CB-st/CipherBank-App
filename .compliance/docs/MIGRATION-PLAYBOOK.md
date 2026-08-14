# Migration playbook

## Principle

Refactoring preserves behavior while changing structure. Modernization may intentionally change behavior, but each change must be named, tested, and reviewed separately. Do not mix framework migration, architecture movement, package replacement, formatting churn, and feature changes in one unreviewable step.

## Phase 0: establish recoverability

1. Work on a branch with a clean status or clearly catalogued existing changes.
2. Record the current SDK, build command, test command, database engine, startup command, and deployment target.
3. Preserve representative input files, API payloads, database schemas, and expected outputs without storing secrets.
4. Capture current package and vulnerability inventory.
5. Define a rollback point.

Exit criterion: the old system can be built or its failure is reproducible and documented.

## Phase 1: inventory structure and behavior

Run the scanner and classify each finding as confirmed, false positive, deferred, or blocked. Then map behaviors—not merely classes—to vertical slices.

For every slice identify:

- trigger: HTTP request, command, UI action, scheduled job, file arrival;
- inputs and their units/encoding/culture;
- observable outputs;
- persistent and external side effects;
- ordering, timing, retry, and transaction expectations;
- failure behavior;
- security and authorization decisions;
- performance and data-volume envelope.

Exit criterion: one slice is small enough to characterize end-to-end.

## Phase 2: characterize original behavior

Add tests around the outermost stable seam. If code cannot be instantiated without its environment, first introduce a narrow seam without changing behavior—for example a clock, file reader, HTTP transport, or repository interface.

Capture both desirable and relied-upon accidental behavior. Label defects explicitly; do not silently “fix” them during a structural move.

Exit criterion: the selected slice has tests that fail when meaningful output or side effects change.

## Phase 3: separate policy from mechanism

Use this ownership test:

| Question | Destination |
|---|---|
| Would this rule exist with a different UI and database? | Core/domain |
| Does this coordinate a user/system use case? | Application |
| Does this speak SQL, HTTP, files, queues, devices, or telemetry? | Infrastructure |
| Does this construct the process or translate transport/UI events? | Host/API/UI |

Introduce inward-owned ports. Move pure decisions before moving adapters. Keep mappings explicit.

Exit criterion: policy tests run without the database, network, filesystem, UI framework, or host.

## Phase 4: modernize implementation mechanics

Apply modern patterns only where they fit the behavior:

- nullable annotations after null behavior is characterized;
- records and strongly typed IDs for immutable values;
- `TimeProvider` for time-sensitive logic;
- async/cancellation for I/O;
- bounded channels for producer-consumer work;
- typed options and startup validation;
- `IHttpClientFactory` and explicit resilience;
- EF migrations and provider-backed tests;
- structured logs, activities, and bounded-cardinality metrics;
- source generation where reflection/AOT cost is material.

Exit criterion: the slice passes characterization and new boundary tests under .NET 10.

## Phase 5: recomposition

Register implementations in one composition root. Establish lifetimes explicitly:

- singleton: thread-safe process-wide stateless/cache/configured objects;
- scoped: request/unit-of-work objects such as `DbContext`;
- transient: lightweight stateless operations where independent instances are useful.

Apply decorators in named order. A typical write pipeline is authorization → validation → idempotency → transaction → metrics/logging → concrete handler, but the correct order depends on semantics and must be tested.

Exit criterion: the complete host starts with scope and options validation.

## Phase 6: verification and cutover

Run:

1. focused unit tests;
2. adapter integration tests;
3. contract/API/UI tests;
4. full Release build and test suite;
5. analyzer and formatting gate;
6. package vulnerability and deprecation audit;
7. representative performance comparison;
8. migration rehearsal against a copy of production-like data;
9. shutdown, cancellation, and recovery exercises.

Exit criterion: old and new behavior agree except for explicitly approved changes, and operational evidence is sufficient for deployment.

## Phase 7: repeat and remove scaffolding

Migrate the next slice. Remove obsolete implementations only after all callers move and rollback requirements expire. Delete compatibility shims rather than allowing them to become permanent architecture.

## Change-set discipline

Prefer this sequence of reviewable changes:

1. characterization tests;
2. seam/interface introduction;
3. pure policy extraction;
4. adapter implementation;
5. composition switch;
6. old-code removal;
7. compiler/analyzer enforcement increase;
8. intentional feature change.

Each step should compile and keep relevant tests green.
