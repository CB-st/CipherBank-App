using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Handlers;

/// <summary>
/// HTTP message handler that automatically injects Bearer token authentication headers.
/// Retrieves the current auth token from IAuthService and adds it to outgoing requests.
/// </summary>
public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthHeaderHandler>? _logger;

    public AuthHeaderHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILogger<AuthHeaderHandler>>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Skip auth header for login/refresh endpoints
        var requestPath = request.RequestUri?.AbsolutePath ?? "";
        if (IsAuthEndpoint(requestPath))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        try
        {
            // Get auth service - use GetService to avoid circular dependency issues during startup
            var authService = _serviceProvider.GetService<IAuthService>();
            if (authService == null)
            {
                _logger?.LogDebug("AuthService not available, proceeding without auth header");
                return await base.SendAsync(request, cancellationToken);
            }

            var token = await authService.GetStoredTokenAsync();
            if (token != null && !string.IsNullOrEmpty(token.AccessToken))
            {
                // Check if token is expired (with 5-minute buffer)
                if (token.ExpiresUtc > DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
#if DEBUG
                    _logger?.LogDebug("Added Bearer token to request for {Uri}", request.RequestUri);
#endif
                }
                else
                {
                    // Token is expired or about to expire, try to refresh
                    _logger?.LogDebug("Token expired or expiring soon, attempting refresh");
                    try
                    {
                        var newToken = await authService.RefreshAsync(token.RefreshToken, cancellationToken);
                        if (newToken != null && !string.IsNullOrEmpty(newToken.AccessToken))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken.AccessToken);
                            _logger?.LogDebug("Refreshed token and added to request");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to refresh token, proceeding without auth header");
                    }
                }
            }
            else
            {
                _logger?.LogDebug("No stored token available for {Uri}", request.RequestUri);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error adding auth header, proceeding without it");
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static bool IsAuthEndpoint(string path)
    {
        return path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase);
    }
}
