# Service templates

Use the interface, implementation, and test templates as one unit. A service
contract belongs in the innermost layer that can express it without platform
types; its implementation belongs in the layer that owns the dependency.

HTTP clients remain adapters behind focused client interfaces. Native Android,
iOS, Mac Catalyst, and Windows implementations stay below `Platforms/` and are
selected only in `MauiProgram`. Stateful development implementations use an
`InMemory*` name; Moq remains test-only.

See `TEMPLATE.md` before registering a new capability.
