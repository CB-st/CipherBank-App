# Host + UI agent contract

CipherBank-app is both the MAUI composition root/host and the views/view-model
layer. It carries both host and UI rules.

## Host

- `MauiProgram` is the composition root. It owns configuration loading and the
  interface-to-implementation map.
- Own startup, dependency composition, middleware/UI lifecycle, transport models,
  and error mapping.
- Keep handlers/controllers/view models thin and delegate policy.
- Validate inputs before domain execution and return consistent errors.
- Propagate request/window cancellation and perform bounded shutdown.
- Mock/simulated implementations are development-only registrations. Release
  builds must resolve production services.
- Configuration defaults come from `config/`; per-user mutable settings may use
  Preferences or SecureStorage through an injected abstraction.
- Do not query database contexts directly when an application port exists.
- Visual tokens and reusable styles live only under `Resources/Styles`. Read that
  subtree's `AGENTS.md` and `docs/style/README.md` before adding or changing UI.

## UI

- Views contain layout and binding only. ViewModels depend on interfaces and do
  not call `Shell.Current`, `SecureStorage`, `Preferences`, or platform APIs.
- Platform implementations stay under `Platforms/<platform>` and implement a
  Core or app service interface.
- Marshal all UI updates through the toolkit's dispatcher; never call `.Result`
  or `.Wait()` on the UI thread.
- Use `async void` only at the outermost event-handler boundary.
- Meet baseline accessibility: every interactive control has a programmatic name,
  the whole surface is keyboard-operable, and color is never the only signal.
- Views use semantic color and typography resources. Literal colors and one-off
  font families do not belong in page XAML.
