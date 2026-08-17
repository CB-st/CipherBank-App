// <copyright file="HttpRateLimiterFactoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.RateLimiting;
using CipherBank_app.Services;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

public sealed class HttpRateLimiterFactoryTests
{
    [Fact]
    public async Task Create_SixtyFirstAcquire_IsRejectedOnFailFastWindow()
    {
        using SlidingWindowRateLimiter limiter = HttpRateLimiterFactory.Create(
            permitLimit: 60,
            window: TimeSpan.FromMinutes(1),
            queueLimit: 0);

        for (int i = 0; i < 60; i++)
        {
            RateLimitLease lease = await limiter.AcquireAsync(1, CancellationToken.None);
            using (lease)
            {
                lease.IsAcquired.Should().BeTrue();
            }
        }

        RateLimitLease rejected = await limiter.AcquireAsync(1, CancellationToken.None);
        using (rejected)
        {
            rejected.IsAcquired.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Create_ShortWindow_ReplenishesAfterExpiry()
    {
        using SlidingWindowRateLimiter limiter = HttpRateLimiterFactory.Create(
            permitLimit: 1,
            window: TimeSpan.FromMilliseconds(50),
            queueLimit: 0);

        RateLimitLease first = await limiter.AcquireAsync(1, CancellationToken.None);
        using (first)
        {
            first.IsAcquired.Should().BeTrue();
        }

        RateLimitLease blocked = await limiter.AcquireAsync(1, CancellationToken.None);
        using (blocked)
        {
            blocked.IsAcquired.Should().BeFalse();
        }

        await Task.Delay(80);
        RateLimitLease afterWindow = await limiter.AcquireAsync(1, CancellationToken.None);
        using (afterWindow)
        {
            afterWindow.IsAcquired.Should().BeTrue();
        }
    }

    [Fact]
    public void Create_NonPositivePermitLimit_Throws()
    {
        Action zero = () => HttpRateLimiterFactory.Create(permitLimit: 0);
        Action negative = () => HttpRateLimiterFactory.Create(permitLimit: -1);
        zero.Should().Throw<ArgumentOutOfRangeException>();
        negative.Should().Throw<ArgumentOutOfRangeException>();
    }
}
