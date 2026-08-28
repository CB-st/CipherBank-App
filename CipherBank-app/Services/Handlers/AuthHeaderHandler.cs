// <copyright file="AuthHeaderHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

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
public sealed partial class AuthHeaderHandler : DelegatingHandler
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
        var requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
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
                if (_logger != null)
                {
                    LogAuthServiceNotAvailable(_logger);
                }

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
                    if (_logger != null)
                    {
                        LogAddedBearerToken(_logger, request.RequestUri);
                    }
#endif
                }
                else
                {
                    // Token is expired or about to expire, try to refresh
                    if (_logger != null)
                    {
                        LogTokenExpiredAttemptingRefresh(_logger);
                    }

                    try
                    {
                        var newToken = await authService.RefreshAsync(token.RefreshToken, cancellationToken);
                        if (newToken != null && !string.IsNullOrEmpty(newToken.AccessToken))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken.AccessToken);

                            if (_logger != null)
                            {
                                LogRefreshedTokenAdded(_logger);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            LogRefreshTokenFailed(_logger, ex);
                        }
                    }
                }
            }
            else
            {
                if (_logger != null)
                {
                    LogNoStoredToken(_logger, request.RequestUri);
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                LogErrorAddingAuthHeader(_logger, ex);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static bool IsAuthEndpoint(string path)
    {
        return path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "AuthService not available, proceeding without auth header")]
    private static partial void LogAuthServiceNotAvailable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Added Bearer token to request for {Uri}")]
    private static partial void LogAddedBearerToken(ILogger logger, Uri? uri);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Token expired or expiring soon, attempting refresh")]
    private static partial void LogTokenExpiredAttemptingRefresh(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refreshed token and added to request")]
    private static partial void LogRefreshedTokenAdded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to refresh token, proceeding without auth header")]
    private static partial void LogRefreshTokenFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No stored token available for {Uri}")]
    private static partial void LogNoStoredToken(ILogger logger, Uri? uri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error adding auth header, proceeding without it")]
    private static partial void LogErrorAddingAuthHeader(ILogger logger, Exception ex);
}
