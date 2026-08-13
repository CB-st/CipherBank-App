// <copyright file="AuthHeaderHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Handlers;

/// <summary>
/// HTTP message handler that injects Bearer tokens from product session or legacy IAuthService.
/// </summary>
public sealed partial class AuthHeaderHandler : DelegatingHandler
{
    // --- Token freshness buffers ---
    private const int ProductTokenSkewMinutes = 1;
    private const int LegacyTokenSkewMinutes = 5;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthHeaderHandler>? _logger;

    private readonly TimeProvider _timeProvider;

    public AuthHeaderHandler(IServiceProvider serviceProvider, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILogger<AuthHeaderHandler>>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (IsUnauthenticatedEndpoint(requestPath))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Prefer product /v1 session (custody unlock path).
        try
        {
            var productSessions = _serviceProvider.GetService<IProductSessionStore>();
            if (productSessions is not null)
            {
                var product = await productSessions.GetAsync().ConfigureAwait(false);
                if (product is { } p && p.Expires > _timeProvider.GetUtcNow().AddMinutes(ProductTokenSkewMinutes))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p.Access);
                    return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            // Fall through to legacy auth service.
        }

        try
        {
            var authService = _serviceProvider.GetService<IAuthService>();
            if (authService == null)
            {
                if (_logger != null)
                {
                    LogAuthServiceNotAvailable(_logger);
                }

                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            var token = await authService.GetStoredTokenAsync().ConfigureAwait(false);
            if (token != null && !string.IsNullOrEmpty(token.AccessToken))
            {
                if (token.ExpiresUtc > _timeProvider.GetUtcNow().AddMinutes(LegacyTokenSkewMinutes))
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
                    if (_logger != null)
                    {
                        LogTokenExpiredAttemptingRefresh(_logger);
                    }

                    try
                    {
                        var newToken = await authService.RefreshAsync(token.RefreshToken, cancellationToken).ConfigureAwait(false);
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
            else if (_logger != null)
            {
                LogNoStoredToken(_logger, request.RequestUri);
            }
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                LogErrorAddingAuthHeader(_logger, ex);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsUnauthenticatedEndpoint(string path)
    {
        return path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/auth/register", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/v1/session", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/v1/session/refresh", StringComparison.OrdinalIgnoreCase);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "AuthService not available, proceeding without auth header")]
    private static partial void LogAuthServiceNotAvailable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Added Bearer token to request for {Uri}")]
    private static partial void LogAddedBearerToken(ILogger logger, Uri? uri);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Token expired or about to expire, attempting refresh")]
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
