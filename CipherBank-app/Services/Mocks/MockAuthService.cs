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
public class MockAuthService : IAuthService
{
    private readonly ILogger<MockAuthService> _logger;
    private AuthToken? _currentToken;
    private RandomNumberGenerator _random = RandomNumberGenerator.Create();

    // Simulated latency range in milliseconds
    private const int MinLatencyMs = 200;
    private const int MaxLatencyMs = 600;

#if DEBUG
    // Test credentials - only available in DEBUG builds
    private const string TestUsername = "testuser";
    private const string TestPassword = "password123";
#endif

    public MockAuthService(ILogger<MockAuthService> logger)
    {
        _logger = logger;
        _logger.LogDebug("MockAuthService initialized");
    }

    public async Task<AuthToken> LoginAsync(string user, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        _logger.LogDebug("Attempting login (mock)");
        await SimulateNetworkDelayAsync(cancellationToken);

#if DEBUG
        // Validate credentials - only accept test credentials in DEBUG builds
        var isValidCredentials = user.Equals(TestUsername, StringComparison.OrdinalIgnoreCase) &&
                                 password == TestPassword;
#else
        // In RELEASE builds, MockAuthService should not be used
        // Always reject authentication to prevent accidental production use
        var isValidCredentials = false;
        _logger.LogError("MockAuthService should not be used in production builds");
#endif

        if (!isValidCredentials)
        {
            _logger.LogWarning("Login failed for user {Username}: Invalid credentials", user);
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Generate mock token
        _currentToken = new AuthToken(
            GenerateJwtToken(user),
            GenerateRefreshToken(),
            DateTimeOffset.UtcNow.AddHours(1));

        _logger.LogInformation("User {Username} logged in successfully. Token expires at {ExpiresUtc}",
            user, _currentToken.ExpiresUtc);

        return _currentToken;
    }

    public async Task<AuthToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        _logger.LogDebug("Refreshing token (mock)");
        await SimulateNetworkDelayAsync(cancellationToken);

        // Validate refresh token exists
        if (_currentToken == null || _currentToken.RefreshToken != refreshToken)
        {
            _logger.LogWarning("Token refresh failed: Invalid refresh token");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Generate new token
        _currentToken = new AuthToken(
            GenerateJwtToken("refreshed_user"),
            GenerateRefreshToken(),
            DateTimeOffset.UtcNow.AddHours(1));

        _logger.LogInformation("Token refreshed successfully. New token expires at {ExpiresUtc}",
            _currentToken.ExpiresUtc);

        return _currentToken;
    }

    public Task<AuthToken?> GetStoredTokenAsync()
    {
        _logger.LogDebug("Getting stored token (mock): {HasToken}", _currentToken != null);
        return Task.FromResult(_currentToken);
    }

    public Task<bool> IsTokenExpiredAsync()
    {
        if (_currentToken == null)
        {
            _logger.LogDebug("No token stored, considering expired");
            return Task.FromResult(true);
        }

        var isExpired = _currentToken.ExpiresUtc <= DateTimeOffset.UtcNow.AddMinutes(5);
        _logger.LogDebug("Token expiration check: IsExpired={IsExpired}, ExpiresUtc={ExpiresUtc}",
            isExpired, _currentToken.ExpiresUtc);
        return Task.FromResult(isExpired);
    }

    public async Task LogoutAsync()
    {
        await RevokeTokenAsync();
        _logger.LogInformation("User logged out (mock)");
        _currentToken = null;
    }

    public async Task<bool> RevokeTokenAsync(CancellationToken cancellationToken = default)
    {
        await SimulateNetworkDelayAsync(cancellationToken);

        if (_currentToken == null)
        {
            _logger.LogDebug("No token to revoke (mock)");
            return true;
        }

        _logger.LogInformation("Token revoked (mock)");
        return true;
    }

    private string GenerateJwtToken(string username)
    {
        // Generate a mock JWT-like token (not a real JWT, just for testing)
        string header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            "{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            $"{{\"sub\":\"{username}\",\"iat\":{DateTimeOffset.UtcNow.ToUnixTimeSeconds()},\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}"));
        string signature = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return $"{header}.{payload}.{signature}";
    }

    private string GenerateRefreshToken()
    {
        byte[] bytes = new byte[32];
        _random.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        int delay = RandomNumberGenerator.GetInt32(MinLatencyMs, MaxLatencyMs);
        await Task.Delay(delay, cancellationToken);
    }
}
