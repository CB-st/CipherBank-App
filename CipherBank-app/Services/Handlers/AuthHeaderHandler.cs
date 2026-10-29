// <copyright file="AuthHeaderHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Handlers;

/// <summary>
/// HTTP message handler that injects Bearer tokens from the product session store.
/// Use: High (every authenticated product HTTP call). Scope: Shell HTTP pipeline.
/// </summary>
public sealed partial class AuthHeaderHandler : DelegatingHandler
{
    private const int ProductTokenSkewMinutes = 1;

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

        try
        {
            IProductSessionStore? productSessions = _serviceProvider.GetService<IProductSessionStore>();
            if (productSessions is not null)
            {
                (string Access, string Refresh, DateTimeOffset Expires)? product = await productSessions.GetAsync().ConfigureAwait(false);
                if (product is { } p && p.Expires > _timeProvider.GetUtcNow().AddMinutes(ProductTokenSkewMinutes))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", p.Access);
                    return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger is not null)
            {
                LogErrorAddingAuthHeader(_logger, ex);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsUnauthenticatedEndpoint(string path)
    {
        return path.EndsWith("/v1/session", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/v1/session/refresh", StringComparison.OrdinalIgnoreCase);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error adding auth header, proceeding without it")]
    private static partial void LogErrorAddingAuthHeader(ILogger logger, Exception ex);
}
