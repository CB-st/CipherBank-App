# Modular feature composition

Each non-trivial vertical slice exposes one explicit
`Add<Feature>Feature(IServiceCollection, IConfiguration)` extension at the MAUI
composition root. The extension binds and validates its options, registers
focused ports and implementations, and registers feature pages and ViewModels.
`MauiProgram` orders these calls; it does not reproduce each feature's service
map.

Registration is deterministic. Do not use reflection, assembly scanning,
runtime dependency bags, static service locators, or service resolution during
registration. Stateful development substitutes use `InMemory*` names and are
selected only at the composition root; `Mock*` remains test-only.

ViewModels consume focused interfaces. Clipboard, preferences, secure storage,
Shell navigation, UI dispatch, and background scheduling belong behind injected
ports. Diagnostic `CB1005` enforces this boundary for direct global calls from
files below `ViewModels/`.

Persistence remains a Core-owned subsystem behind focused repositories. Seed
initialization, market hydration, and scheduling are separate capabilities so a
future feature can depend on only the operation it needs.
