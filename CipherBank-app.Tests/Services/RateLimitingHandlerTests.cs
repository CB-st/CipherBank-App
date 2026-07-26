// <copyright file="RateLimitingHandlerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using CipherBank_app.Services;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

/// <summary>
/// Unit tests for RateLimiter integration scenarios.
/// Note: Full RateLimitingHandler tests require the MAUI app context.
/// These tests verify the core rate limiting logic.
/// </summary>
public class RateLimitingHandlerTests
{
    [Fact]
    public async Task RateLimiter_HighVolumeScenario_EnforcesLimit()
    {
        // Arrange - Simulate high-volume request scenario
        var rateLimiter = new RateLimiter(null, 100, TimeSpan.FromMinutes(1));

        // Act - Make 100 requests (the limit)
        var successCount = 0;
        for (int i = 0; i < 100; i++)
        {
            if (await rateLimiter.TryAcquireAsync(default))
            {
                successCount++;
            }
        }

        // Assert - All 100 should succeed
        successCount.Should().Be(100);

        // 101st request should fail
        var overLimit = await rateLimiter.TryAcquireAsync(default);
        overLimit.Should().BeFalse();
    }

    [Fact]
    public async Task RateLimiter_ConcurrentRequests_ThreadSafe()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 50, TimeSpan.FromMinutes(1));
        var tasks = new Task<bool>[100];

        // Act - Make 100 concurrent requests
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = rateLimiter.TryAcquireAsync(default);
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Exactly 50 should succeed (the limit)
        var successCount = results.Count(r => r);
        successCount.Should().Be(50);
    }

    [Fact]
    public async Task RateLimiter_BurstThenWait_ResetsCorrectly()
    {
        // Arrange - Short window for testing
        var rateLimiter = new RateLimiter(null, 5, TimeSpan.FromMilliseconds(200));

        // Act - Burst of requests
        for (int i = 0; i < 5; i++)
        {
            await rateLimiter.TryAcquireAsync(default);
        }

        // Should be at limit
        rateLimiter.CurrentRequestCount.Should().Be(5);
        (await rateLimiter.TryAcquireAsync(default)).Should().BeFalse();

        // Wait for window to expire
        await Task.Delay(250);

        // Should be able to make requests again
        var afterWait = await rateLimiter.TryAcquireAsync(default);
        afterWait.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimiter_GetWaitTime_ProvidesAccurateEstimate()
    {
        // Arrange
        var windowDuration = TimeSpan.FromMilliseconds(500);
        var rateLimiter = new RateLimiter(null, 1, windowDuration);

        // Act
        await rateLimiter.TryAcquireAsync(default);
        var waitTime = await rateLimiter.GetWaitTimeAsync(default);

        // Assert - Wait time should be close to window duration
        waitTime.Should().BeGreaterThan(TimeSpan.Zero);
        waitTime.Should().BeLessThanOrEqualTo(windowDuration);
    }
}
