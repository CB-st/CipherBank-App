# AuthToken

## Purpose

Immutable value record carrying the access token, refresh token, and access-token expiry returned by authentication operations.

## Source
- `CipherBank-app.Core/Models/AuthToken.cs`

## Contract

Positional members: `AccessToken` (`string`), `RefreshToken` (`string`), and `ExpiresUtc` (`DateTimeOffset`). The record declares no additional validation or computed members. It is serialized/deserialized by `AuthService` and read by `AuthHeaderHandler`.

## Dependencies/Inputs

Constructed from token strings and an expiry timestamp; no collaborators.

## Failure behavior

The type defines no failure behavior or validation.

## Tests/Validation
- [`AuthTokenTests.cs`](../../CipherBank-app.Tests/Models/AuthTokenTests.cs)

## Related docs
- [AuthService](../app/AuthService.md)
- [AuthHeaderHandler](../app/AuthHeaderHandler.md)
