using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Handlers;

/// <summary>
/// HTTP message handler that applies rate limiting to outgoing requests.
/// Uses a sliding window algorithm to enforce request limits.
/// </summary>
public sealed class RateLimitingHandler : DelegatingHandler
{
    private readonly RateLimiter _rateLimiter;
    private readonly ILogger<RateLimitingHandler>? _logger;

    /// <summary>
    /// Maximum time to wait for rate limit to clear before timing out.
    /// </summary>
    private static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(30);

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
            _logger?.LogWarning("Rate limit exceeded, wait time {WaitTime} exceeds maximum {MaxWait}",
                waitTime, MaxWaitTime);

            return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                ReasonPhrase = "Rate limit exceeded",
                Content = new StringContent("Too many requests. Please try again later.")
            };
        }

        if (waitTime > TimeSpan.Zero)
        {
            _logger?.LogDebug("Rate limited, waiting {WaitTime} before retry", waitTime);
            await Task.Delay(waitTime, cancellationToken);
        }

        // Try again after waiting
        if (await _rateLimiter.TryAcquireAsync(cancellationToken))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Still rate limited, return 429
        _logger?.LogWarning("Rate limit still exceeded after waiting");
        return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            ReasonPhrase = "Rate limit exceeded",
            Content = new StringContent("Too many requests. Please try again later.")
        };
    }
}
