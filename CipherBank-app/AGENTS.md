# MAUI Host Contract

- `MauiProgram` is the composition root. It owns configuration loading and the
  interface-to-implementation map.
- Views contain layout and binding only. ViewModels depend on interfaces and do
  not call `Shell.Current`, `SecureStorage`, `Preferences`, or platform APIs.
- Platform implementations stay under `Platforms/<platform>` and implement a
  Core or app service interface.
- Mock/simulated implementations are development-only registrations. Release
  builds must resolve production services.
- Configuration defaults come from `config/`; per-user mutable settings may use
  Preferences or SecureStorage through an injected abstraction.
