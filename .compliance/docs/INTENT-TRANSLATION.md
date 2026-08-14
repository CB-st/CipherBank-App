# Translating original intent into refactored code

## Why intent is easy to lose

Legacy code often combines the rule, its input cleanup, database access, logging, UI state, retry behavior, and output formatting. A mechanical “clean architecture” move can preserve lines while changing ordering, transaction scope, rounding, null treatment, or failure semantics. Translate behavior before translating structure.

## Intent worksheet

Complete one copy per migrated behavior.

### 1. User/system intent

- Actor or triggering system:
- Trigger:
- Desired outcome:
- Why the behavior exists:
- What the caller considers success:

### 2. Inputs

| Input | Type/shape | Required | Unit/culture/encoding | Valid range | Default |
|---|---|---:|---|---|---|
| | | | | | |

### 3. Outputs and side effects

| Output/side effect | Observable contract | Ordering | Transactional | Idempotent |
|---|---|---|---:|---:|
| | | | | |

### 4. Rules and invariants

- Preconditions:
- Calculations and rounding:
- State transitions:
- Authorization decisions:
- Duplicate handling:
- Empty/null behavior:
- Time-zone and clock behavior:

### 5. Failure semantics

| Failure | Old behavior | Intended behavior | Stable error/code | Retryable |
|---|---|---|---|---:|
| | | | | |

### 6. Operational envelope

- Typical and maximum input size:
- Expected latency/throughput:
- Concurrency:
- Shutdown behavior:
- Data retention/security classification:

### 7. Evidence

- Existing tests:
- Representative fixtures:
- Production logs/metrics:
- Documentation or stakeholder decision:
- Known defects intentionally retained or changed:

## Translation method

### Step A: name the behavior as a verb

Prefer `ImportMeasurements`, `AuthorizeTransfer`, or `CalculateConcentration` over class-shaped names such as `MeasurementManager`.

### Step B: separate facts from mechanisms

Convert framework objects into plain facts at the boundary. An `HttpRequest`, `DataRow`, or UI control is a mechanism; species, value, unit, actor, and timestamp are facts.

### Step C: define the result before the implementation

Model success and expected failures explicitly. Do not use exceptions for ordinary validation or not-found outcomes. Keep result codes independent of HTTP status, database exceptions, or UI messages.

### Step D: extract policy

Move calculations, decisions, state transitions, and invariants into deterministic Core code. Supply time and randomness explicitly. The policy should run in a unit test without building a host.

### Step E: define ports from the consumer's needs

Do not mirror an entire SDK or database table. Define the smallest operation the use case requires, such as `FindAsync`, `SaveAsync`, or `ReserveAsync`.

### Step F: implement adapters

Map external representations, translate expected external failures, propagate cancellation, and retain the original transaction/order behavior unless a reviewed change says otherwise.

### Step G: compose and compare

Run the same fixtures through old and new implementations. Compare returned values, serialized output, persistent state, emitted events, and error behavior.

## Common translation errors

| Mistake | Consequence | Correction |
|---|---|---|
| Moving database entities into Core | Persistence details become domain contracts | Introduce domain types and explicit mapping |
| Adding interfaces for every class | Indirection without a boundary | Add ports only for variable/external mechanisms |
| Replacing all exceptions with one result | Infrastructure faults become ordinary outcomes | Separate expected domain failures from exceptional faults |
| Making everything async | Noise and allocation without asynchronous work | Keep pure CPU policy synchronous |
| Retrying every HTTP request | Duplicate state changes | Retry transient idempotent operations only |
| Enabling nullable then adding `!` | Warnings disappear without understanding null behavior | Characterize null cases and correct types/guards |
| Splitting by technical folders only | A behavior still spans hidden coupling | Migrate and test by vertical slice |
| “Fixing” rounding during movement | Scientific/financial output changes silently | Freeze reference values, approve change separately |

## Refactor acceptance record

- Characterization tests added:
- New Core policy and ports:
- Adapters added:
- Composition change:
- Approved behavior differences:
- Unit/integration/contract tests:
- Performance comparison:
- Rollback method:
- Reviewer and evidence:
