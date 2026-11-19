# AuthHeaderHandler

## Purpose

Adds a Bearer access token to outgoing requests handled by this `DelegatingHandler`, refreshing a near-expiry token when possible.

## Source
- `CipherBank-app/Services/Handlers/AuthHeaderHandler.cs`

## Contract

`SendAsync(request, cancellationToken)` bypasses requests whose absolute path contains `/auth/login`, `/auth/refresh`, or `/auth/register` (case-insensitive). For other paths it resolves `IAuthService` from the supplied service provider. A token with expiry more than five minutes away is attached as `Authorization: Bearer <access token>`. Otherwise, it attempts `RefreshAsync` and attaches a returned non-empty access token. It then forwards the request.

Fail-closed behavior is pending in #111; current behavior proceeds without an auth header when the service/token is unavailable or token refresh/header preparation fails.

## Dependencies/Inputs

Constructor takes a non-null `IServiceProvider`; it resolves `IAuthService` and optionally `ILogger<AuthHeaderHandler>`. Inputs are the request and cancellation token.

## Failure behavior

Auth lookup, token retrieval, and refresh exceptions are caught and logged when a logger is available; the handler continues to `base.SendAsync` without adding a header. In the current `IAuthService`-unavailable branch, the early `base.SendAsync` call is inside the `try`; if it throws, the catch logs it and execution falls through to a second, final `base.SendAsync` call, whose exceptions propagate. Cancellation passed to refresh is forwarded.

## Tests/Validation
- Gap: no dedicated `AuthHeaderHandler` test file was found under `CipherBank-app.Tests`.

## Related docs
- [AuthService](AuthService.md)
- [AuthToken](../core/AuthToken.md)
- [App services guide](services.md)
