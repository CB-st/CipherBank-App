// <copyright file="AuthService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services;

/// <summary>
/// Production implementation of IAuthService using HTTP client.
/// </summary>
public sealed partial class AuthService(ILogger<AuthService> logger, HttpClient http, AuthSessionGuard sessionGuard)
    : IAuthService
{
    private const string SessionEndedMessage = "Session ended before the token could be stored";
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";
    private const string ExpiresUtcKey = "auth_expires_utc";

    public async Task<AuthToken> LoginAsync(string user, string password, CancellationToken cancellationToken = default)
    {
        int generation = await sessionGuard.CaptureAsync(cancellationToken);
        HttpResponseMessage resp = await http.PostAsJsonAsync("auth/login", new { user, password }, cancellationToken);
        resp.EnsureSuccessStatusCode();
        AuthToken? token = await resp.Content.ReadFromJsonAsync<AuthToken>(cancellationToken: cancellationToken);
        if (token == null)
        {
            LogDeserializeAuthTokenFailed(logger);
            throw new InvalidOperationException("Authentication failed: Unable to parse token from server response");
        }

        await StoreTokenIfCurrentAsync(token, generation, cancellationToken, startsSession: true);

        LogUserAuthenticated(logger);
        return token;
    }

    public async Task<AuthToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        int generation = await sessionGuard.CaptureRefreshAsync(
            refreshToken,
            () => SecureStorage.Default.GetAsync(RefreshTokenKey),
            cancellationToken);
        HttpResponseMessage resp = await http.PostAsJsonAsync("auth/refresh", new { refreshToken }, cancellationToken);
        resp.EnsureSuccessStatusCode();
        AuthToken? token = await resp.Content.ReadFromJsonAsync<AuthToken>(cancellationToken: cancellationToken);
        if (token == null)
        {
            LogDeserializeRefreshTokenFailed(logger);
            throw new InvalidOperationException("Token refresh failed: Unable to parse token from server response");
        }

        await StoreTokenIfCurrentAsync(token, generation, cancellationToken);

        LogTokenRefreshed(logger);
        return token;
    }

    public async Task<AuthToken?> GetStoredTokenAsync()
    {
        try
        {
            var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            var expiresUtcString = await SecureStorage.Default.GetAsync(ExpiresUtcKey);

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(expiresUtcString))
            {
                return null;
            }

            if (!DateTimeOffset.TryParse(expiresUtcString, out DateTimeOffset expiresUtc))
            {
                return null;
            }

            return new AuthToken(accessToken, refreshToken, expiresUtc);
        }
        catch (Exception ex)
        {
            LogRetrieveStoredTokenFailed(logger, ex);
            return null;
        }
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        AuthToken? token = await GetStoredTokenAsync();
        if (token == null)
        {
            return true;
        }

        return token.ExpiresUtc <= DateTimeOffset.UtcNow.AddMinutes(5); // 5 minute buffer
    }

    public Task<int> CaptureSessionAsync() => sessionGuard.CaptureAsync();

    public async Task LogoutAsync()
    {
        await LogoutCoreAsync();
    }

    public Task<bool> LogoutAsync(int capturedGeneration) => LogoutCoreAsync(capturedGeneration);

    public async Task<bool> RevokeTokenAsync(CancellationToken cancellationToken = default)
    {
        AuthToken? token = await GetStoredTokenAsync();
        return await RevokeRefreshTokenAsync(token?.RefreshToken, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize authentication token from API response")]
    private static partial void LogDeserializeAuthTokenFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "User authenticated successfully")]
    private static partial void LogUserAuthenticated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize refresh token from API response")]
    private static partial void LogDeserializeRefreshTokenFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Token refreshed successfully")]
    private static partial void LogTokenRefreshed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to retrieve stored authentication token")]
    private static partial void LogRetrieveStoredTokenFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to revoke token during logout")]
    private static partial void LogRevokeTokenDuringLogoutFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "User logged out")]
    private static partial void LogUserLoggedOut(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No token to revoke")]
    private static partial void LogNoTokenToRevoke(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Token revoked successfully")]
    private static partial void LogTokenRevoked(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Token revocation failed with status {StatusCode}")]
    private static partial void LogTokenRevocationFailed(ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HTTP error during token revocation")]
    private static partial void LogHttpErrorDuringRevocation(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error during token revocation")]
    private static partial void LogUnexpectedErrorDuringRevocation(ILogger logger, Exception ex);

    private async Task<bool> LogoutCoreAsync(int? capturedGeneration = null)
    {
        var session = await sessionGuard.CaptureLogoutAsync(
            async () => (await GetStoredTokenAsync())?.RefreshToken, capturedGeneration);
        if (!session.HasValue)
        {
            return false;
        }

        // Attempt to revoke token before clearing local storage
        try
        {
            await RevokeRefreshTokenAsync(session.Value.RefreshToken);
        }
        catch (Exception ex)
        {
            // Log but don't fail logout if revocation fails
            LogRevokeTokenDuringLogoutFailed(logger, ex);
        }

        bool cleared = await sessionGuard.InvalidateAndClearAsync(
            () =>
            {
                SecureStorage.Default.Remove(AccessTokenKey);
                SecureStorage.Default.Remove(RefreshTokenKey);
                SecureStorage.Default.Remove(ExpiresUtcKey);
                return Task.CompletedTask;
            },
            capturedGeneration: session.Value.Generation);
        LogUserLoggedOut(logger);
        return cleared;
    }

    private async Task<bool> RevokeRefreshTokenAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                LogNoTokenToRevoke(logger);
                return true;
            }

            HttpResponseMessage resp = await http.PostAsJsonAsync("auth/revoke", new { RefreshToken = refreshToken }, cancellationToken);

            if (resp.IsSuccessStatusCode)
            {
                LogTokenRevoked(logger);
                return true;
            }

            LogTokenRevocationFailed(logger, resp.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorDuringRevocation(logger, ex);
            return false;
        }
        catch (Exception ex)
        {
            LogUnexpectedErrorDuringRevocation(logger, ex);
            return false;
        }
    }

    private async Task StoreTokenIfCurrentAsync(AuthToken token, int generation, CancellationToken cancellationToken, bool startsSession = false)
    {
        bool stored = await sessionGuard.TryPersistAsync(
            generation,
            async () =>
            {
                await SecureStorage.Default.SetAsync(AccessTokenKey, token.AccessToken);
                await SecureStorage.Default.SetAsync(RefreshTokenKey, token.RefreshToken);
                await SecureStorage.Default.SetAsync(ExpiresUtcKey, token.ExpiresUtc.ToString("O"));
            },
            startsSession: startsSession,
            cancellationToken: cancellationToken);

        if (!stored)
        {
            throw new InvalidOperationException(SessionEndedMessage);
        }
    }
}
