// <copyright file="MockAuthService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Mocks;

/// <summary>
/// Mock implementation of IAuthService for development and testing.
/// Provides simulated authentication without making actual API calls.
/// </summary>
public sealed partial class MockAuthService : IAuthService
{
    // Simulated latency range in milliseconds
    private const int MinLatencyMs = 200;
    private const int MaxLatencyMs = 600;

#if DEBUG
    // Test credentials - only available in DEBUG builds
    private const string TestUsername = "testuser";
    private const string TestPassword = "password123";
#endif

    private readonly ILogger<MockAuthService> _logger;

    private AuthToken? _currentToken;

    private readonly TimeProvider _timeProvider;

    public MockAuthService(ILogger<MockAuthService> logger, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _logger = logger;
        LogMockAuthServiceInitialized(_logger);
    }

    public async Task<AuthToken> LoginAsync(string user, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        LogAttemptingLogin(_logger);
        await SimulateNetworkDelayAsync(cancellationToken);

#if DEBUG
        // Validate credentials - only accept test credentials in DEBUG builds
        var isValidCredentials = user.Equals(TestUsername, StringComparison.OrdinalIgnoreCase) &&
                                 password == TestPassword;
#else
        // In RELEASE builds, MockAuthService should not be used
        // Always reject authentication to prevent accidental production use
        var isValidCredentials = false;
        LogMockAuthServiceInProduction(_logger);
#endif

        if (!isValidCredentials)
        {
            LogLoginFailed(_logger, user);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Generate mock token
        _currentToken = new AuthToken(
            GenerateJwtToken(user),
            GenerateRefreshToken(),
            _timeProvider.GetUtcNow().AddHours(1));

        LogLoginSucceeded(_logger, user, _currentToken.ExpiresUtc);

        return _currentToken;
    }

    public async Task<AuthToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        LogRefreshingToken(_logger);
        await SimulateNetworkDelayAsync(cancellationToken);

        // Validate refresh token exists
        if (_currentToken == null || _currentToken.RefreshToken != refreshToken)
        {
            LogRefreshTokenInvalid(_logger);
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Generate new token
        _currentToken = new AuthToken(
            GenerateJwtToken("refreshed_user"),
            GenerateRefreshToken(),
            _timeProvider.GetUtcNow().AddHours(1));

        LogTokenRefreshedSuccessfully(_logger, _currentToken.ExpiresUtc);

        return _currentToken;
    }

    public Task<AuthToken?> GetStoredTokenAsync()
    {
        LogGettingStoredToken(_logger, _currentToken != null);
        return Task.FromResult(_currentToken);
    }

    public Task<bool> IsTokenExpiredAsync()
    {
        if (_currentToken == null)
        {
            LogNoTokenStoredConsideringExpired(_logger);
            return Task.FromResult(true);
        }

        var isExpired = _currentToken.ExpiresUtc <= _timeProvider.GetUtcNow().AddMinutes(5);
        LogTokenExpirationCheck(_logger, isExpired, _currentToken.ExpiresUtc);
        return Task.FromResult(isExpired);
    }

    public async Task LogoutAsync()
    {
        await RevokeTokenAsync();
        LogUserLoggedOut(_logger);
        _currentToken = null;
    }

    public async Task<bool> RevokeTokenAsync(CancellationToken cancellationToken = default)
    {
        await SimulateNetworkDelayAsync(cancellationToken);

        if (_currentToken == null)
        {
            LogNoTokenToRevoke(_logger);
            return true;
        }

        LogTokenRevoked(_logger);
        return true;
    }

    /// <summary>
    /// Builds a mock JWT-shaped string stamped with this service's clock.
    /// Use: Medium (mock login/refresh). Scope: MockAuthService instance clock.
    /// </summary>
    private string GenerateJwtToken(string username)
    {
        // Generate a mock JWT-like token (not a real JWT, just for testing)
        string header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            "{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            $"{{\"sub\":\"{username}\",\"iat\":{_timeProvider.GetUtcNow().ToUnixTimeSeconds()},\"exp\":{_timeProvider.GetUtcNow().AddHours(1).ToUnixTimeSeconds()}}}"));
        string signature = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return $"{header}.{payload}.{signature}";
    }

    private static string GenerateRefreshToken()
    {
        byte[] bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        int delay = RandomNumberGenerator.GetInt32(MinLatencyMs, MaxLatencyMs);
        await Task.Delay(delay, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "MockAuthService initialized")]
    private static partial void LogMockAuthServiceInitialized(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Attempting login (mock)")]
    private static partial void LogAttemptingLogin(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "MockAuthService should not be used in production builds")]
    private static partial void LogMockAuthServiceInProduction(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed for user {Username}: Invalid credentials")]
    private static partial void LogLoginFailed(ILogger logger, string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {Username} logged in successfully. Token expires at {ExpiresUtc}")]
    private static partial void LogLoginSucceeded(ILogger logger, string username, DateTimeOffset expiresUtc);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refreshing token (mock)")]
    private static partial void LogRefreshingToken(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Token refresh failed: Invalid refresh token")]
    private static partial void LogRefreshTokenInvalid(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Token refreshed successfully. New token expires at {ExpiresUtc}")]
    private static partial void LogTokenRefreshedSuccessfully(ILogger logger, DateTimeOffset expiresUtc);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting stored token (mock): {HasToken}")]
    private static partial void LogGettingStoredToken(ILogger logger, bool hasToken);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No token stored, considering expired")]
    private static partial void LogNoTokenStoredConsideringExpired(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Token expiration check: IsExpired={IsExpired}, ExpiresUtc={ExpiresUtc}")]
    private static partial void LogTokenExpirationCheck(ILogger logger, bool isExpired, DateTimeOffset expiresUtc);

    [LoggerMessage(Level = LogLevel.Information, Message = "User logged out (mock)")]
    private static partial void LogUserLoggedOut(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No token to revoke (mock)")]
    private static partial void LogNoTokenToRevoke(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Token revoked (mock)")]
    private static partial void LogTokenRevoked(ILogger logger);
}
