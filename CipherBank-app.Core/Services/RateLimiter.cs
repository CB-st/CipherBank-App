// <copyright file="RateLimiter.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

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
public sealed partial class RateLimiter : IDisposable
{
    /// <summary>Default max requests per sliding window (1/sec average over one minute).</summary>
    private const int DefaultMaxRequestsPerWindow = 60;

    private readonly ILogger<RateLimiter>? _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentQueue<DateTimeOffset> _requestTimestamps = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RateLimiter()
        : this(null, DefaultMaxRequestsPerWindow, TimeSpan.FromMinutes(1), null)
    {
    }

    public RateLimiter(ILogger<RateLimiter>? logger)
        : this(logger, DefaultMaxRequestsPerWindow, TimeSpan.FromMinutes(1), null)
    {
    }

    public RateLimiter(ILogger<RateLimiter>? logger, int maxRequests, TimeSpan windowDuration)
        : this(logger, maxRequests, windowDuration, null)
    {
    }

    public RateLimiter(
        ILogger<RateLimiter>? logger,
        int maxRequests,
        TimeSpan windowDuration,
        TimeProvider? timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        MaxRequests = maxRequests > 0 ? maxRequests : throw new ArgumentOutOfRangeException(nameof(maxRequests), "Must be positive");
        WindowDuration = windowDuration > TimeSpan.Zero ? windowDuration : throw new ArgumentOutOfRangeException(nameof(windowDuration), "Must be positive");

        if (_logger is not null)
        {
            LogRateLimiterInitialized(_logger, maxRequests, windowDuration);
        }
    }

    /// <summary>
    /// Maximum number of requests allowed per window. Default: 60.
    /// </summary>
    public int MaxRequests { get; }

    /// <summary>
    /// Time window duration. Default: 1 minute.
    /// </summary>
    public TimeSpan WindowDuration { get; }

    /// <summary>
    /// Gets the current number of requests in the sliding window.
    /// </summary>
    public int CurrentRequestCount => _requestTimestamps.Count;

    /// <summary>
    /// Attempts to acquire a permit to make a request.
    /// Returns true if the request is allowed, false if rate limited.
    /// </summary>
    public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            PruneExpired(now);

            if (_requestTimestamps.Count >= MaxRequests)
            {
                if (_logger is not null)
                {
                    LogRateLimitExceeded(_logger, _requestTimestamps.Count, MaxRequests);
                }

                return false;
            }

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
    public async Task<TimeSpan> GetWaitTimeAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            PruneExpired(now);

            if (_requestTimestamps.Count < MaxRequests)
            {
                return TimeSpan.Zero;
            }

            if (_requestTimestamps.TryPeek(out DateTimeOffset oldestInWindow))
            {
                TimeSpan waitTime = (oldestInWindow + WindowDuration) - now;
                return waitTime > TimeSpan.Zero ? waitTime : TimeSpan.Zero;
            }

            return TimeSpan.Zero;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _lock.Dispose();

    [LoggerMessage(Level = LogLevel.Information, Message = "RateLimiter initialized: {MaxRequests} requests per {WindowDuration}")]
    private static partial void LogRateLimiterInitialized(ILogger logger, int maxRequests, TimeSpan windowDuration);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit exceeded: {Count}/{Max} requests in window")]
    private static partial void LogRateLimitExceeded(ILogger logger, int count, int max);

    /// <summary>
    /// Drops timestamps that fall outside the current sliding window.
    /// Use: High (TryAcquire / GetWaitTime). Scope: this limiter instance.
    /// </summary>
    private void PruneExpired(DateTimeOffset now)
    {
        DateTimeOffset windowStart = now - WindowDuration;
        while (_requestTimestamps.TryPeek(out DateTimeOffset oldest) && oldest < windowStart)
        {
            _requestTimestamps.TryDequeue(out _);
        }
    }
}
