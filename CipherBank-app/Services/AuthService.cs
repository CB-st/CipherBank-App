using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace CipherBank_app.Services;

public class AuthService(ILogger<AuthService> logger, HttpClient http)
    : IAuthService
{
    const string AccessTokenKey = "auth_access_token";
    const string RefreshTokenKey = "auth_refresh_token";
    const string ExpiresUtcKey = "auth_expires_utc";


    public async Task<AuthToken> LoginAsync(string user, string password, CancellationToken cancellationToken = default)
    {
        var resp = await http.PostAsJsonAsync("auth/login", new { user, password}, cancellationToken);
        resp.EnsureSuccessStatusCode();
        var token = await resp.Content.ReadFromJsonAsync<AuthToken>(cancellationToken: cancellationToken);
        if (token == null)
        {
            logger.LogError("Failed to deserialize authentication token from API response");
            throw new InvalidOperationException("Authentication failed: Unable to parse token from server response");
        }

        // Store tokens securely
        await SecureStorage.Default.SetAsync(AccessTokenKey, token.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, token.RefreshToken);
        await SecureStorage.Default.SetAsync(ExpiresUtcKey, token.ExpiresUtc.ToString("O"));

        http.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        logger.LogInformation("User authenticated successfully");
        return token;
    }

    public async Task<AuthToken> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var resp = await http.PostAsJsonAsync("auth/refresh", new { refreshToken }, cancellationToken);
        resp.EnsureSuccessStatusCode();
        var token = await resp.Content.ReadFromJsonAsync<AuthToken>(cancellationToken: cancellationToken);
        if (token == null)
        {
            logger.LogError("Failed to deserialize refresh token from API response");
            throw new InvalidOperationException("Token refresh failed: Unable to parse token from server response");
        }

        // Store refreshed tokens securely
        await SecureStorage.Default.SetAsync(AccessTokenKey, token.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, token.RefreshToken);
        await SecureStorage.Default.SetAsync(ExpiresUtcKey, token.ExpiresUtc.ToString("O"));

        http.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        logger.LogInformation("Token refreshed successfully");
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
                return null;

            if (!DateTimeOffset.TryParse(expiresUtcString, out var expiresUtc))
                return null;

            return new AuthToken(accessToken, refreshToken, expiresUtc);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve stored authentication token");
            return null;
        }
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        var token = await GetStoredTokenAsync();
        if (token == null)
            return true;

        return token.ExpiresUtc <= DateTimeOffset.UtcNow.AddMinutes(5); // 5 minute buffer
    }

    public async Task LogoutAsync()
    {
        // Attempt to revoke token before clearing local storage
        try
        {
            await RevokeTokenAsync();
        }
        catch (Exception ex)
        {
            // Log but don't fail logout if revocation fails
            logger.LogWarning(ex, "Failed to revoke token during logout");
        }

        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(ExpiresUtcKey);
        http.DefaultRequestHeaders.Authorization = null;
        logger.LogInformation("User logged out");
    }

    public async Task<bool> RevokeTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetStoredTokenAsync();
            if (token == null)
            {
                logger.LogDebug("No token to revoke");
                return true;
            }

            var resp = await http.PostAsJsonAsync("auth/revoke", new { token.RefreshToken }, cancellationToken);

            if (resp.IsSuccessStatusCode)
            {
                logger.LogInformation("Token revoked successfully");
                return true;
            }

            logger.LogWarning("Token revocation failed with status {StatusCode}", resp.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error during token revocation");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during token revocation");
            return false;
        }
    }
}