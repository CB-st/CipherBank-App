# SonarQube-aligned development standard

This standard turns analyzer feedback into a repeatable construction method for C# functions and objects. SonarQube remains the authoritative evaluator of its rules. The overlay scanner only inventories migration risks and must not be used to claim a Sonar quality-gate pass.

## Required quality model

Use Clean as You Code: new and changed code must meet the gate even while historical debt is retired separately. Start with the built-in **Sonar way** C# quality profile. Create a derived profile only when a documented project constraint requires a rule or parameter change.

The minimum new-code gate is:

| Condition | Required result |
|---|---:|
| New issues | 0 |
| New Security Hotspots reviewed | 100% |
| Coverage on new code | at least 80% |
| Duplicated lines on new code | at most 3% |

Do not add an aggregate Cognitive Complexity metric condition. That total naturally grows as a system grows. Enforce method-level rule **S3776** instead; use the active C# profile's threshold (commonly 15) as the maximum, and refactor the method that raises the issue. A property/accessor threshold can be stricter where the active profile defines it.

## Function construction template

Build a function in this order:

1. **Name one outcome.** The name describes the decision or side effect, not a vague activity such as `Process`.
2. **Declare the contract.** Make input, output, nullability, failure, cancellation, units, culture, time, and side effects explicit.
3. **Reject invalid state early.** Guard clauses keep the valid path shallow. Do not add a single-exit requirement.
4. **Separate decisions from mechanisms.** Put pure branching/calculation in a small function; put I/O and orchestration in an async use-case function.
5. **Keep one readable level of abstraction.** A coordinator should read as a short sequence of named operations, not alternate between business policy and low-level parsing/SQL.
6. **Prefer closed decisions.** Use a switch expression, pattern matching, or a strategy when cases are known. Avoid boolean mode parameters that make one method behave as two.
7. **Bound work.** Propagate `CancellationToken`, set timeouts, bound queues and payload size, and avoid hidden retries.
8. **Return an explicit outcome.** Expected rejection uses a result/domain error; infrastructure failure stays exceptional. Do not return `null`, `false`, or an empty collection for unrelated failure meanings.
9. **Instrument the boundary.** Log stable templates and safe identifiers at the coordinator/adapter boundary, not every helper.
10. **Test the decision table.** Cover the happy path, each guard, every branch, important boundaries, and each expected failure.

### Complexity refactor order

When S3776 fires, preserve behavior with characterization tests, then apply the smallest useful transformations:

1. invert a condition and return early;
2. extract a pure named decision;
3. replace repeated condition chains with a switch or lookup;
4. split orchestration from parsing, validation, authorization, and persistence;
5. replace type/mode branching with polymorphism only when variants have stable independent behavior;
6. remove duplication only after the extracted concept has a meaningful name.

Do not game the score with meaningless forwarding methods, partial classes, rule suppression, or a move to another file. Cognitive Complexity is a maintainability signal, not a runtime-performance measure.

## Object construction template

Every class or record should have one stated responsibility and one reason to change.

| Concern | Construction rule | Verification |
|---|---|---|
| Invariants | Create valid objects through a constructor/factory; use immutable value objects for important concepts | invalid construction tests |
| Dependencies | Constructor-inject narrow inward-owned ports; no service locator or ambient mutable state | DI composition test |
| Cohesion | Keep data with the behavior that preserves it; split unrelated capabilities | public API and responsibility review |
| Encapsulation | Expose intent-bearing methods, not mutable internal collections/state | mutation/invariant tests |
| I/O | Keep database, HTTP, filesystem, queue, and clock mechanics in adapters | provider-backed integration tests |
| Lifetime | Make ownership and disposal explicit; never capture scoped services in singletons | scope validation and shutdown tests |
| Extensibility | Prefer composition; introduce an interface for a real boundary or multiple implementation need | contract tests across implementations |
| Serialization | Map transport/persistence types at boundaries; do not make them domain models | contract and mapping tests |

Large constructor dependency counts, broad interfaces, repeated `IServiceProvider` access, and classes whose methods operate on disjoint fields are design-review triggers. Do not split a cohesive object solely to hit an arbitrary line count.

## Security construction standard

Treat a Sonar vulnerability as an actionable issue. Treat a Security Hotspot as a mandatory review: determine whether the sensitive operation is safe in its real context and record the review outcome and reasoning.

For every trust boundary:

- validate type, length/range, format, encoding, canonical path/URI, and allowed values;
- authorize the resource and action server-side after authentication;
- use parameterized APIs for SQL/commands/templates and allowlists for dynamic identifiers;
- avoid shell execution; if unavoidable, pass structured arguments and fixed executables;
- use safe serializers, disable dangerous type activation, and bound object depth/size;
- keep secrets out of source, logs, errors, URLs, fixtures, and generated artifacts;
- use platform cryptography and managed key storage; never invent algorithms or static IVs/nonces;
- constrain outbound destinations to prevent SSRF and revalidate redirects/resolved addresses when relevant;
- canonicalize file paths and confirm they remain under an allowed root before access;
- set request, body, stream, archive, collection, retry, and concurrency limits;
- return safe client errors while preserving diagnostic detail only in protected telemetry.

Security fixes require a negative test that proves the rejected attack shape and a positive test that preserves valid behavior.

## Suppression and exception policy

A suppression is allowed only when the code is safe or the rule does not apply. Record:

- Sonar rule key and exact location;
- why the rule is a false positive or accepted risk;
- supporting test or threat-model evidence;
- owner, review date, and expiration/revisit condition.

Prefer a narrow attribute or reviewed suppression file. Do not use blanket file/project exclusions, broad `NoWarn`, or quality-profile deactivation to make a gate green. Generated code and third-party vendored code may be excluded only with an explicit ownership reason.

## Pull-request checklist

- [ ] The function/object has one stated responsibility and explicit contract.
- [ ] Valid flow is shallow; guards, branches, and failure meanings are obvious.
- [ ] S3776 and all other new-code issues are clear in the authoritative Sonar analysis.
- [ ] New/changed policy is covered at boundaries and every meaningful branch.
- [ ] New Security Hotspots are reviewed and security-sensitive changes include negative tests.
- [ ] New-code coverage is at least 80% and duplication is at most 3%.
- [ ] No suppression or exclusion lacks scoped justification, ownership, and review evidence.
- [ ] The PR quality gate passes before merge.

## Official references

- SonarQube quality gates: <https://docs.sonarsource.com/sonarqube-server/quality-standards-administration/managing-quality-gates/introduction-to-quality-gates>
- C# Cognitive Complexity rule S3776: <https://rules.sonarsource.com/csharp/RSPEC-3776/>
- Quality profiles: <https://docs.sonarsource.com/sonarqube-server/quality-standards-administration/managing-quality-profiles/understanding-quality-profiles>
- Security-related rules: <https://docs.sonarsource.com/sonarqube-server/2026.1/quality-standards-administration/managing-rules/security-related-rules>
- Security Hotspots: <https://docs.sonarsource.com/sonarqube-server/user-guide/security-hotspots>
