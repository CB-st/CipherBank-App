# UI agent contract

- Keep views/components declarative: layout, style, and bindings only; no business rules and no direct Infrastructure/persistence calls.
- Keep view models/components thin: translate one user gesture into one Application call and expose presentation state (busy, validation, selection).
- Marshal all UI updates through the toolkit's dispatcher/synchronization context; never call `.Result`/`.Wait()` on the UI thread.
- Use `async void` only at the outermost event-handler boundary; everything it calls returns `Task`/`Task<T>` and is independently testable.
- Propagate cancellation scoped to the interaction or window/page lifetime; report progress instead of manipulating controls from a background thread.
- Meet baseline accessibility: every interactive control has a programmatic name, the whole surface is keyboard-operable, and color is never the only signal.
- Scope DI lifetimes to the view/page/window that owns the resource; do not hold UI state in a singleton across navigations.
- Test view models/components against Application ports independent of the UI framework; test dispatcher-specific behavior separately.

See `.compliance/docs/UI-COMPOSITION.md` for the full method.
