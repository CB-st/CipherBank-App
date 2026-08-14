# Infrastructure agent contract

- Implement Core/Application ports for databases, HTTP, files, queues, clocks, telemetry, and devices (including GPU compute).
- Map external DTOs/entities explicitly at the boundary.
- Validate typed configuration at startup.
- Use real provider/protocol integration tests.
- Use `AsNoTracking` for read-only EF queries and migrations in deployed systems.
- Use `IHttpClientFactory`, bounded timeouts, and retries only for transient idempotent work.
- Keep third-party types and exceptions from leaking inward.
- Own the full lifetime of pooled/native/device buffers: check device availability, fall back to the Core reference implementation when unavailable, and release every allocation on every exit path, including exceptions.
