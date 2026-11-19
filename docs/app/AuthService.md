# AuthService

## Purpose

Implements `IAuthService` for login, token refresh, local token retrieval/expiry checks, logout, and best-effort server revocation.

## Source
- `CipherBank-app/Services/AuthService.cs`
- Contract: `CipherBank-app.Core/Services/IAuthService.cs`

## Contract

- `LoginAsync(user, password, cancellationToken)` posts credentials to `auth/login`, deserializes and stores an `AuthToken`, and returns it.
- `RefreshAsync(refreshToken, cancellationToken)` posts to `auth/refresh`, stores and returns the replacement token.
- `GetStoredTokenAsync()` returns a token only when all three stored values exist and expiry parses; otherwise returns `null`.
- `IsTokenExpiredAsync()` returns `true` when no valid stored token exists or expiry is within five minutes of UTC now.
- `LogoutAsync()` attempts revocation, then removes all three secure-storage entries.
- `RevokeTokenAsync(cancellationToken)` returns `true` if there is no token or the server succeeds; returns `false` for unsuccessful status or caught failures.

### Login and refresh

Both operations serialize anonymous request objects, require a successful HTTP status, deserialize `AuthToken`, and write access token, refresh token, and round-trip expiry to secure storage. A null response body result throws `InvalidOperationException`. HTTP, serialization, cancellation, or secure-storage failures can propagate.

## Dependencies/Inputs

Injected `ILogger<AuthService>` and `HttpClient`; MAUI `SecureStorage.Default`; login credentials, refresh token, and optional cancellation tokens. Storage keys are `auth_access_token`, `auth_refresh_token`, and `auth_expires_utc`.

## Failure behavior

Login and refresh propagate non-success HTTP status exceptions and other operation failures. Stored-token retrieval catches any exception, logs, and returns `null`; malformed or incomplete storage also returns `null`. Revocation catches failures and returns `false`. Logout catches revocation failures, then removes local keys.

## Tests/Validation
- [`AuthServiceTests.cs`](../../CipherBank-app.Tests/Services/AuthServiceTests.cs) exercises the `IAuthService` contract through Moq; it does not instantiate the production HTTP/storage implementation.

## Related docs
- [AuthToken](../core/AuthToken.md)
- [App services guide](services.md)
- [AuthHeaderHandler](AuthHeaderHandler.md)
