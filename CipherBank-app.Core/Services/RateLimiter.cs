using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services;

/// <summary>
/// Thread-safe sliding window rate limiter.
/// Limits requests to a configurable number per time window.
/// </summary>
public sealed class RateLimiter
{
    private readonly ILogger<RateLimiter>? _logger;
    private readonly ConcurrentQueue<DateTimeOffset> _requestTimestamps = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Maximum number of requests allowed per window. Default: 60.
    /// </summary>
    public int MaxRequests { get; }

    /// <summary>
    /// Time window duration. Default: 1 minute.
    /// </summary>
    public TimeSpan WindowDuration { get; }

    public RateLimiter() : this(null, 60, TimeSpan.FromMinutes(1))
    {
    }

    public RateLimiter(ILogger<RateLimiter>? logger) : this(logger, 60, TimeSpan.FromMinutes(1))
    {
    }

    public RateLimiter(ILogger<RateLimiter>? logger, int maxRequests, TimeSpan windowDuration)
    {
        _logger = logger;
        MaxRequests = maxRequests > 0 ? maxRequests : throw new ArgumentOutOfRangeException(nameof(maxRequests), "Must be positive");
        WindowDuration = windowDuration > TimeSpan.Zero ? windowDuration : throw new ArgumentOutOfRangeException(nameof(windowDuration), "Must be positive");

        _logger?.LogInformation("RateLimiter initialized: {MaxRequests} requests per {WindowDuration}", maxRequests, windowDuration);
    }

    /// <summary>
    /// Attempts to acquire a permit to make a request.
    /// Returns true if the request is allowed, false if rate limited.
    /// </summary>
    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var windowStart = now - WindowDuration;

            // Remove expired timestamps
            while (_requestTimestamps.TryPeek(out var oldest) && oldest < windowStart)
            {
                _requestTimestamps.TryDequeue(out _);
            }

            // Check if we're at the limit
            if (_requestTimestamps.Count >= MaxRequests)
            {
                _logger?.LogWarning("Rate limit exceeded: {Count}/{Max} requests in window", _requestTimestamps.Count, MaxRequests);
                return false;
            }

            // Add the new request timestamp
            _requestTimestamps.Enqueue(now);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets the time to wait before the next request can be made.
    /// Returns TimeSpan.Zero if a request can be made immediately.
    /// </summary>
    public async Task<TimeSpan> GetWaitTimeAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var windowStart = now - WindowDuration;

            // Remove expired timestamps
            while (_requestTimestamps.TryPeek(out var oldest) && oldest < windowStart)
            {
                _requestTimestamps.TryDequeue(out _);
            }

            if (_requestTimestamps.Count < MaxRequests)
            {
                return TimeSpan.Zero;
            }

            // Get the oldest timestamp that's still in the window
            if (_requestTimestamps.TryPeek(out var oldestInWindow))
            {
                var waitUntil = oldestInWindow + WindowDuration;
                var waitTime = waitUntil - now;
                return waitTime > TimeSpan.Zero ? waitTime : TimeSpan.Zero;
            }

            return TimeSpan.Zero;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets the current number of requests in the sliding window.
    /// </summary>
    public int CurrentRequestCount => _requestTimestamps.Count;
}
