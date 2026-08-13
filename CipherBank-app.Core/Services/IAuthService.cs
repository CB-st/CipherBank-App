// <copyright file="IAuthService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for user authentication and token management.
/// </summary>
public interface IAuthService
{
    Task<AuthToken> LoginAsync(string user, string password, CancellationToken cancellationToken = default);

    Task<AuthToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<AuthToken?> GetStoredTokenAsync();

    Task<bool> IsTokenExpiredAsync();

    Task LogoutAsync();

    /// <summary>
    /// Revokes the current access and refresh tokens on the server.
    /// Should be called before logout to invalidate tokens server-side.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if revocation succeeded, false otherwise</returns>
    Task<bool> RevokeTokenAsync(CancellationToken cancellationToken = default);
}
