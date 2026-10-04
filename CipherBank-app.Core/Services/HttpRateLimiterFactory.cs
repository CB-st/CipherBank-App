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
    public const int DefaultPermitLimit = 60;

    public static SlidingWindowRateLimiter Create(
        int permitLimit = DefaultPermitLimit,
        TimeSpan? window = null,
        int queueLimit = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        ArgumentOutOfRangeException.ThrowIfNegative(queueLimit);
        TimeSpan resolvedWindow = window ?? TimeSpan.FromMinutes(1);
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
