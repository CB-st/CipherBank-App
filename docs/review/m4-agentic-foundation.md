# M4 agentic foundation

This change converts the M1a-M4 conventions from passive guidance into a dispatchable build system while preserving the existing product, cryptographic, persistence, design-system, and Appium boundaries.

## Implemented foundation

| Area | Implementation |
| --- | --- |
| Workflow routing | `config/agentic/dispatch.json` maps feature, service, UI, data, device, and validation work to focused skills, templates, contracts, follow-ups, and gates |
| Dispatch packets | `templates/dispatch/` plus `scripts/create-dispatch.py` create bounded, secret-free work orders |
| Feature construction | `templates/feature/` defines one explicit DI composition entry point for a vertical slice |
| Shared resources | `docs/agentic/RESOURCE_OWNERSHIP.md` and `templates/resource/` separate global owners from feature-local derived resources |
| DI/object construction | `docs/agentic/MODULE_COMPOSITION.md` defines interface placement, lifetimes, registration order, and prohibited locator/bag patterns |
| Enforcement | Static structure validation and `AgenticDispatchTests` require stable workflows, existing referenced paths, CipherBank skill names, and non-empty gates |
| Agent skills | Dispatcher, feature, UI, data, E2E, and stack-validation skills provide bounded implementation workflows |

## Preserved contracts

- Core, ChallengePass, MAUI, persistence, and E2E dependencies continue to point inward.
- Runtime services use focused interfaces and constructor injection; feature modules remain explicit composition-time registration code.
- EF Core owns routine persistence and `LocalDbSql` remains the only compatibility SQL owner.
- Global typography, semantic color, and component styles remain canonical; feature resources derive from them and promote only after cross-feature reuse.
- ChallengePass/custody zeroization, fused A2 identity, offline behavior, stable Appium stories, and sensitive-artifact rules are unchanged.
- Central package ownership, project assembly metadata, warning policy, and Sonar scope are unchanged.

## Verification boundary

The repository structure gate validates the new dispatch schema, referenced resources, templates, documentation, JSON, XML/XAML resources, and Python dispatcher syntax. The dispatcher has been exercised across every configured workflow, including its no-overwrite behavior. The `AgenticDispatchTests` test the same contract in .NET when the SDK is available.
