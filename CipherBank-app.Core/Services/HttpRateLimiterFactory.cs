// <copyright file="HttpRateLimiterFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.RateLimiting;

namespace CipherBank_app.Services;

/// <summary>
/// Builds the shared sliding-window limiter used by Shell HTTP resilience.
/// QueueLimit 0 is fail-fast (no 30s wait).
/// Use: High (every outbound product/public HTTP). Scope: Http.Resilience.
/// </summary>
public static class HttpRateLimiterFactory
{
    private const int DefaultPermitLimit = 60;

    /// <summary>Creates the default limiter: 60 permits per minute, fail-fast.</summary>
    /// <returns>A new sliding-window limiter.</returns>
    public static SlidingWindowRateLimiter Create() => Create(DefaultPermitLimit);

    /// <summary>Creates a fail-fast limiter with a one-minute window.</summary>
    /// <param name="permitLimit">Permits per window.</param>
    /// <returns>A new sliding-window limiter.</returns>
    public static SlidingWindowRateLimiter Create(int permitLimit) => Create(permitLimit, TimeSpan.FromMinutes(1), 0);

    /// <summary>Creates a limiter with explicit settings.</summary>
    /// <param name="permitLimit">Permits per window.</param>
    /// <param name="window">Window length; must be positive.</param>
    /// <param name="queueLimit">Queued acquisitions allowed; 0 is fail-fast.</param>
    /// <returns>A new sliding-window limiter.</returns>
    public static SlidingWindowRateLimiter Create(int permitLimit, TimeSpan window, int queueLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        ArgumentOutOfRangeException.ThrowIfNegative(queueLimit);
        TimeSpan resolvedWindow = window;
        if (resolvedWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "Window must be positive.");
        }

        return new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = resolvedWindow,
            SegmentsPerWindow = 1,
            QueueLimit = queueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    }
}
