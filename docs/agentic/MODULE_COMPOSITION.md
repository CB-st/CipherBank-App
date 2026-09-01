# Modular feature composition

## Construction model

A feature is a vertical slice with explicit inward dependencies:

| Concern | Owner | Connection |
| --- | --- | --- |
| Domain model or port | `CipherBank-app.Core/<Feature>/` | Focused interface injected into consumers |
| Platform-neutral implementation | `CipherBank-app.Core/<Feature>/` | Registered by an `Add<Feature>Feature` extension |
| Crypto protocol implementation | `CipherBank-app.ChallengePass/<Feature>/` | Exposed through a ChallengePass port; never pulled into Core |
| MAUI adapter, page, or ViewModel | `CipherBank-app/` | Constructor injection at the MAUI composition root |
| Configuration | `config/<theme>/` plus typed options in owner | Defaults loaded first and validated on startup |
| Persistent state | `CipherBank-app.Core/Persist/` | EF Core repository behind a focused port |
| Unit/E2E verification | Owning test project | Moq for collaborators; Appium selectors only in page objects |

## Module entry point

Use one explicit `Add<Feature>Feature(IServiceCollection, IConfiguration)` extension for each non-trivial vertical slice. The method binds and validates options, registers interface-to-implementation mappings, and registers page/ViewModel lifetimes when the module is MAUI-owned.

Composition methods are deterministic registration code. Do not use reflection, assembly scanning, static service locators, global mutable registries, or runtime dependency bags. Do not resolve services while registering them unless an implementation selection genuinely depends on another registered option/service and the cycle is documented.

Runtime objects receive only the focused dependencies they use. If a constructor grows beyond a coherent responsibility, extract a coordinator or capability interface rather than wrapping parameters in a bag.

## Lifetimes

- `Singleton`: stateless thread-safe services, device-wide custody/session state with explicit lifecycle, repositories designed for concurrent use, and `TimeProvider`.
- `Transient`: pages, ViewModels, request-scoped coordinators, and lightweight operations with no retained state.
- `Scoped`: use only where an explicit scope is created and tested; MAUI does not create web-request scopes.
- Typed HTTP clients: use the existing HTTP registration extensions and handlers; do not construct `HttpClient` directly.

Stateful development substitutes use `InMemory*` and are selected only at the composition root. `Mock*` remains test-only. Platform adapters implement Core-owned interfaces and stay in the platform/application layer.

## Registration order

1. Add repository-owned configuration defaults.
2. Bind and validate typed options.
3. Register inner Core services and repositories.
4. Register ChallengePass and network adapters.
5. Register feature coordinators.
6. Register ViewModels and pages.
7. Build the application; fail startup on invalid required options.

Copy `templates/feature/FeatureModule.cs.template` for a new composition entry point. Update `MauiProgram` with one call to the module rather than scattering registrations through unrelated sections.
