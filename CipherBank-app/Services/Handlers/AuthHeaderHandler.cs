// <copyright file="AuthHeaderHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.Http.Headers;
using CipherBank_app.Models;
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
        _logger = serviceProvider.GetService(typeof(ILogger<AuthHeaderHandler>)) as ILogger<AuthHeaderHandler>;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (IsAuthEndpoint(ExtractPath(request.RequestUri)))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        try
        {
            string token = await ResolveBearerTokenAsync(request.RequestUri, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

    private static string ExtractPath(Uri? requestUri)
    {
        if (requestUri is { IsAbsoluteUri: true })
        {
            return requestUri.AbsolutePath;
        }

        string raw = requestUri?.OriginalString ?? string.Empty;
        int cut = raw.IndexOfAny(['?', '#']);
        string path = cut >= 0 ? raw[..cut] : raw;
        return "/" + path.TrimStart('/');
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

    private async Task<string> ResolveBearerTokenAsync(Uri? requestUri, CancellationToken cancellationToken)
    {
        IAuthService? authService = _serviceProvider.GetService(typeof(IAuthService)) as IAuthService;
        if (authService is null)
        {
            if (_logger is not null)
            {
                LogAuthServiceNotAvailable(_logger);
            }

            throw new HttpRequestException("Authentication service is unavailable.");
        }

        AuthToken? token = await authService.GetStoredTokenAsync();
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            if (_logger is not null)
            {
                LogNoStoredToken(_logger, requestUri);
            }

            throw new HttpRequestException("No access token is available for this request.");
        }

        return token.ExpiresUtc <= DateTimeOffset.UtcNow.AddMinutes(5)
            ? await RefreshTokenAsync(authService, token, cancellationToken)
            : token.AccessToken;
    }

    private async Task<string> RefreshTokenAsync(
        IAuthService authService,
        AuthToken currentToken,
        CancellationToken cancellationToken)
    {
        if (_logger is not null)
        {
            LogTokenExpiredAttemptingRefresh(_logger);
        }

        if (string.IsNullOrWhiteSpace(currentToken.RefreshToken))
        {
            return FailRefresh();
        }

        AuthToken? refreshed = await authService.RefreshAsync(currentToken.RefreshToken, cancellationToken);
        if (refreshed is null || string.IsNullOrWhiteSpace(refreshed.AccessToken) ||
            refreshed.ExpiresUtc <= DateTimeOffset.UtcNow)
        {
            return FailRefresh();
        }

        if (_logger is not null)
        {
            LogRefreshedTokenAdded(_logger);
        }

        return refreshed.AccessToken;
    }

    private string FailRefresh()
    {
        if (_logger is not null)
        {
            LogRefreshTokenFailed(_logger);
        }

        throw new HttpRequestException("Token refresh did not provide a usable access token.");
    }
}
