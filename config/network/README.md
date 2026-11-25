# Network configuration

`endpoints.json` owns the default product API, public quote API, and WebSocket endpoints for each
deployment environment. `NetworkOptions` binds and validates the file during
startup. Production, sandbox, and development require encrypted transports;
only the explicit `Local` profile may use `http`/`ws`.

User preferences select an environment or override its endpoints. Secrets,
tokens, certificate material, and account identifiers never belong here.
