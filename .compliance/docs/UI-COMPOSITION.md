# UI composition and accessibility

## Principle

UI is a Host-layer concern. It translates user and device events into Application use cases and renders the result. It does not own business rules, and it never blocks the UI/dispatcher thread. This applies the same ownership test as `MIGRATION-PLAYBOOK.md` Phase 3: a validation rule belongs in Core/domain regardless of which toolkit renders it.

This document is framework-neutral. It applies to WPF, WinUI 3, .NET MAUI, Avalonia, WinForms, and Blazor (Server and WebAssembly) alike; framework-specific mechanics are named only where the rule would otherwise be unclear.

## Where UI code belongs

| Concern | Layer |
|---|---|
| Layout, style, markup/XAML/Razor, binding declarations | Host/UI (`AGENTS.ui.md`) |
| View model / component state for one interaction (busy flag, validation messages, selection) | Host/UI (`AGENTS.ui.md`) |
| Validation rules, calculations, invariants | Core |
| Orchestrating a use case (the same call a controller or worker would make) | Application |
| Navigation, DI scope, composition root, platform-head startup | Host |

Do not assign a layer based on the project name; a `*.UI` project can still contain Application-shaped orchestration by accident. Inspect what the code actually does, per `AGENT-PLACEMENT.md`.

## Framework-neutral rules

- One composition root per process, the same as any other executable. Views/pages/components resolve dependencies from a scope, not from a static locator.
- View models/components depend on Application ports and commands, not on Infrastructure or transport types directly.
- Views bind to view-model-owned, UI-shaped state; they do not call Infrastructure or open a database connection.
- Map between domain/Core types and UI-bound state explicitly at the boundary, the same discipline `INTENT-TRANSLATION.md` applies to transport DTOs.

## Dispatcher and threading

- Every UI toolkit marshals updates back to the UI thread through a captured `SynchronizationContext` or a `Dispatcher`/`DispatcherQueue`. Never call `.Result` or `.Wait()` on the UI thread: the awaited continuation often needs that same thread, and the call deadlocks instead of returning.
- `async void` is acceptable only at the outermost event-handler boundary — a button click, a page-loaded event. Everything that handler calls should return `Task`/`Task<T>` and be testable without a UI framework (see `LEGACY-PATTERN-MAP.md`).
- Work started from a UI action that performs I/O or can run materially long must accept and observe a `CancellationToken` scoped to the interaction or the window/page lifetime, and should report progress through `IProgress<T>` or an observable view-model property — never by mutating controls from a background thread.
- Blazor Server and Blazor WebAssembly marshal through an equivalent mechanism (`InvokeAsync`) even though there is no window `Dispatcher`; the same blocking rule applies there.

## MVVM / MVU ownership

- **View**: layout, style, and binding declarations only.
- **View model / component state**: presentation state and orchestration calls into Application; no direct persistence, HTTP, or file access.
- **Commands** (`ICommand`, `RelayCommand`, MAUI `Command`, a Blazor `EventCallback`) translate one user gesture into one Application call. They are not where business rules live.
- Prefer source-generated observable properties/commands (for example the `[ObservableProperty]`/`[RelayCommand]` pattern) over hand-written `INotifyPropertyChanged` boilerplate once the generator package has been through `PACKAGE-SELECTION.md`; do not add it as an unreviewed default dependency.
- See `examples/UiCommand/` for a worked refactor of a legacy code-behind handler into a thin, source-generated view model that calls the existing `ImportMeasurementHandler` Application service from `examples/MeasurementSlice/`.

## Data binding and validation

- Bind to view-model-owned, UI-shaped state, not directly to domain/Core types; map explicitly, the same as any transport DTO.
- Re-run Core/Application validation on submit; do not let a validation attribute or converter become the only place a rule is enforced.
- Treat culture, numeric parsing, and date formatting explicitly. Do not rely on the ambient thread culture for anything that is persisted or transmitted.

## Accessibility

- Every interactive control has a programmatic name (automation/accessible name), not only a visual label.
- The entire surface is reachable and operable by keyboard alone; verify tab order and focus visibility.
- Color is never the only signal for state or error; check contrast against WCAG 2.2 AA.
- Respect the OS-level reduced-motion and high-contrast settings where the toolkit exposes them.
- Treat an accessibility regression the same as a functional regression: it gets a test, not a follow-up ticket.

## Navigation and composition

- One composition root per process; pages/views/components resolve dependencies from a scope, not from a static locator or `IServiceProvider` reached for outside composition infrastructure (`AGENTS.root.md`).
- Scope lifetimes to the unit that matches disposal: a page/window scope for its own resources, not a singleton holding UI state across navigations.
- Keep platform heads (WinUI/Android/iOS/Mac Catalyst under MAUI) thin translators into the shared UI/Application layers; do not fork business logic per platform.

## Testing

- Unit test view models/components against Application ports with fakes/substitutes, independent of any UI framework (`TESTING-PLAYBOOK.md`, "API and UI testing").
- Exercise dispatcher-specific behavior — marshaling, cancellation, busy-state transitions — with a UI automation or component test harness kept separate from policy tests.
- Add a regression test for every bug fixed in a converter, formatter, or binding path; these are easy to silently reintroduce because they rarely show up in a Core/Application unit test.

## Related

- `LEGACY-PATTERN-MAP.md` for the code-behind and `async void` migration rows.
- `AGENTS.ui.md` for the enforceable per-project contract.
- `COMPLIANCE-CHECKLIST.md` for the UI evidence items.
- `examples/UiCommand/` for a worked example.
