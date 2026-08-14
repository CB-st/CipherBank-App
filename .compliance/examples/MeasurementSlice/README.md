# Intent-preserving measurement refactor

This example starts with a common legacy shape: one method parses input, checks a remote catalog, performs domain validation, writes through EF, reads wall-clock time, and returns a transport-shaped boolean.

The refactor separates:

- Core: measurement value, validation, result, and ports.
- Application: ordering and use-case orchestration.
- Infrastructure: EF and HTTP mechanics.
- API: request/response mapping and composition.
- Tests: characterization, unit, integration, and API boundaries.

The example files are reference snippets and are not added to the receiving solution automatically.

## Preserved intent

- Species must exist in the catalog before storage.
- Values cannot be negative.
- Duplicate sample IDs update the existing row.
- Capture time is UTC.
- Expected rejection is distinct from unavailable infrastructure.

## Deliberate improvements

- Cancellation is propagated.
- Time is injectable.
- HTTP lifetime and resilience are centrally configured.
- Domain rejection is explicit.
- Persistence and transport types no longer define the domain.
- Each boundary can be tested with the appropriate mechanism.

Read files in numeric order.
