// <copyright file="RateLimitingHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Handlers;

/// <summary>
/// HTTP message handler that applies rate limiting to outgoing requests.
/// Uses a sliding window algorithm to enforce request limits.
/// </summary>
public sealed partial class RateLimitingHandler : DelegatingHandler
{
    /// <summary>
    /// Maximum time to wait for rate limit to clear before timing out.
    /// </summary>
    private static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(30);

    private readonly RateLimiter _rateLimiter;
    private readonly ILogger<RateLimitingHandler>? _logger;

    public RateLimitingHandler(IServiceProvider serviceProvider)
    {
        _rateLimiter = serviceProvider.GetRequiredService<RateLimiter>();
        _logger = serviceProvider.GetService<ILogger<RateLimitingHandler>>();
    }

    public RateLimitingHandler(RateLimiter rateLimiter, ILogger<RateLimitingHandler>? logger = null)
    {
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Try to acquire a permit
        if (await _rateLimiter.TryAcquireAsync(cancellationToken))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Get wait time and check if it's acceptable
        var waitTime = await _rateLimiter.GetWaitTimeAsync(cancellationToken);

        if (waitTime > MaxWaitTime)
        {
            if (_logger != null)
            {
                LogRateLimitExceeded(_logger, waitTime, MaxWaitTime);
            }

            return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                ReasonPhrase = "Rate limit exceeded",
                Content = new StringContent("Too many requests. Please try again later."),
            };
        }

        if (waitTime > TimeSpan.Zero)
        {
            if (_logger != null)
            {
                LogRateLimitedWaiting(_logger, waitTime);
            }

            await Task.Delay(waitTime, cancellationToken);
        }

        // Try again after waiting
        if (await _rateLimiter.TryAcquireAsync(cancellationToken))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Still rate limited, return 429
        if (_logger != null)
        {
            LogRateLimitStillExceeded(_logger);
        }

        return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            ReasonPhrase = "Rate limit exceeded",
            Content = new StringContent("Too many requests. Please try again later."),
        };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit exceeded, wait time {WaitTime} exceeds maximum {MaxWait}")]
    private static partial void LogRateLimitExceeded(ILogger logger, TimeSpan waitTime, TimeSpan maxWait);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Rate limited, waiting {WaitTime} before retry")]
    private static partial void LogRateLimitedWaiting(ILogger logger, TimeSpan waitTime);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit still exceeded after waiting")]
    private static partial void LogRateLimitStillExceeded(ILogger logger);
}
