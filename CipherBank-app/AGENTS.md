# Host + UI agent contract

CipherBank-app is both the MAUI composition root/host *and* the
views/view-model layer — per `.compliance/docs/AGENT-PLACEMENT.md`, that
means it carries both `AGENTS.host.md` and `AGENTS.ui.md` from
`.compliance/templates/`, combined below.

## Host

Applies to `MauiProgram`, `Platforms/`, `Extensions/`, and the DI/service
registration surface (`Services/` where it's composition, not policy).

- Own startup, dependency composition, middleware/UI lifecycle, transport models, and error mapping.
- Keep handlers/controllers/view models thin and delegate policy.
- Validate inputs before domain execution and return consistent errors.
- Propagate request/window cancellation and perform bounded shutdown.
- Keep authentication and authorization distinct from validation.
- Test middleware/routing or UI behavior through the real host boundary.
- Do not query database contexts directly when an application port exists.

## UI

Applies to `Views/`, `ViewModels/`, `Controls/`, and `Converters/`.

- Keep views/components declarative: layout, style, and bindings only; no business rules and no direct Infrastructure/persistence calls.
- Keep view models/components thin: translate one user gesture into one Application call and expose presentation state (busy, validation, selection).
- Marshal all UI updates through the toolkit's dispatcher/synchronization context; never call `.Result`/`.Wait()` on the UI thread.
- Use `async void` only at the outermost event-handler boundary; everything it calls returns `Task`/`Task<T>` and is independently testable.
- Propagate cancellation scoped to the interaction or window/page lifetime; report progress instead of manipulating controls from a background thread.
- Meet baseline accessibility: every interactive control has a programmatic name, the whole surface is keyboard-operable, and color is never the only signal.
- Scope DI lifetimes to the view/page/window that owns the resource; do not hold UI state in a singleton across navigations.
- Test view models/components against Application ports independent of the UI framework; test dispatcher-specific behavior separately.

See `.compliance/docs/UI-COMPOSITION.md` for the full method.
