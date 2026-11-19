// <copyright file="AuthHeaderHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.Http.Headers;
using CipherBank_app.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Handlers;

/// <summary>
/// HTTP message handler that attaches credentials to protected requests.
/// Authentication endpoints remain anonymous; every other request fails before transport
/// if a stored or refreshed access token cannot be attached.
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
        var requestPath = request.RequestUri is { IsAbsoluteUri: true } uri
            ? uri.AbsolutePath
            : "/" + (request.RequestUri?.OriginalString ?? string.Empty).TrimStart('/');
        if (IsAuthEndpoint(requestPath))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        try
        {
            IAuthService? authService = _serviceProvider.GetService<IAuthService>();
            if (authService == null)
            {
                if (_logger != null)
                {
                    LogAuthServiceNotAvailable(_logger);
                }

                throw new HttpRequestException("Authentication service is unavailable.");
            }

            AuthToken? token = await authService.GetStoredTokenAsync();
            if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                if (_logger != null)
                {
                    LogNoStoredToken(_logger, request.RequestUri);
                }

                throw new HttpRequestException("No access token is available for this request.");
            }

            if (token.ExpiresUtc <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (_logger != null)
                {
                    LogTokenExpiredAttemptingRefresh(_logger);
                }

                if (string.IsNullOrWhiteSpace(token.RefreshToken))
                {
                    if (_logger != null)
                    {
                        LogRefreshTokenFailed(_logger);
                    }

                    throw new HttpRequestException("Token refresh did not provide a usable access token.");
                }

                token = await authService.RefreshAsync(token.RefreshToken, cancellationToken);
                if (token == null || string.IsNullOrWhiteSpace(token.AccessToken) ||
                    token.ExpiresUtc <= DateTimeOffset.UtcNow)
                {
                    if (_logger != null)
                    {
                        LogRefreshTokenFailed(_logger);
                    }

                    throw new HttpRequestException("Token refresh did not provide a usable access token.");
                }

                if (_logger != null)
                {
                    LogRefreshedTokenAdded(_logger);
                }
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            if (_logger != null)
            {
                LogAddedBearerToken(_logger, request.RequestUri);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                LogErrorAddingAuthHeader(_logger, ex);
            }

            throw new HttpRequestException("Unable to authenticate this request.", ex);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static bool IsAuthEndpoint(string path)
    {
        string normalized = path.TrimEnd('/');
        return normalized.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
               normalized.EndsWith("/auth/register", StringComparison.OrdinalIgnoreCase);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "AuthService not available; request will fail")]
    private static partial void LogAuthServiceNotAvailable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Added Bearer token to request for {Uri}")]
    private static partial void LogAddedBearerToken(ILogger logger, Uri? uri);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Token expired or expiring soon, attempting refresh")]
    private static partial void LogTokenExpiredAttemptingRefresh(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refreshed token and added to request")]
    private static partial void LogRefreshedTokenAdded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Token refresh did not provide a usable access token")]
    private static partial void LogRefreshTokenFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No stored token available for {Uri}")]
    private static partial void LogNoStoredToken(ILogger logger, Uri? uri);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error adding auth header; request will fail")]
    private static partial void LogErrorAddingAuthHeader(ILogger logger, Exception ex);
}
