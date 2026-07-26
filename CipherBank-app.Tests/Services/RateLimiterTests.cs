// <copyright file="RateLimiterTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using CipherBank_app.Services;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

/// <summary>
/// Unit tests for the RateLimiter class.
/// </summary>
public class RateLimiterTests
{
    [Fact]
    public async Task TryAcquireAsync_UnderLimit_ReturnsTrue()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 10, TimeSpan.FromMinutes(1));

        // Act
        bool result = await rateLimiter.TryAcquireAsync(default);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_AtLimit_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 3, TimeSpan.FromMinutes(1));

        // Act - Make 3 requests (the limit)
        await rateLimiter.TryAcquireAsync(default);
        await rateLimiter.TryAcquireAsync(default);
        await rateLimiter.TryAcquireAsync(default);

        // 4th request should fail
        bool result = await rateLimiter.TryAcquireAsync(default);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_AfterWindowExpires_ReturnsTrue()
    {
        // Arrange - Very short window
        var rateLimiter = new RateLimiter(null, 1, TimeSpan.FromMilliseconds(50));

        // Act - Make request, wait for window, make another
        await rateLimiter.TryAcquireAsync(default);
        await Task.Delay(100); // Wait for window to expire
        bool result = await rateLimiter.TryAcquireAsync(default);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetWaitTimeAsync_WhenUnderLimit_ReturnsZero()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 10, TimeSpan.FromMinutes(1));

        // Act
        TimeSpan waitTime = await rateLimiter.GetWaitTimeAsync(default);

        // Assert
        waitTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetWaitTimeAsync_WhenAtLimit_ReturnsPositive()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 1, TimeSpan.FromSeconds(10));

        // Act
        await rateLimiter.TryAcquireAsync(default); // Use up the limit
        TimeSpan waitTime = await rateLimiter.GetWaitTimeAsync(default);

        // Assert
        waitTime.Should().BeGreaterThan(TimeSpan.Zero);
        waitTime.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task CurrentRequestCount_TracksRequests()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 10, TimeSpan.FromMinutes(1));

        // Act
        await rateLimiter.TryAcquireAsync(default);
        await rateLimiter.TryAcquireAsync(default);
        await rateLimiter.TryAcquireAsync(default);

        // Assert
        rateLimiter.CurrentRequestCount.Should().Be(3);
    }

    [Fact]
    public void Constructor_WithInvalidMaxRequests_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimiter(null, 0, TimeSpan.FromMinutes(1)));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimiter(null, -1, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Constructor_WithInvalidWindowDuration_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimiter(null, 10, TimeSpan.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimiter(null, 10, TimeSpan.FromMinutes(-1)));
    }

    [Fact]
    public void MaxRequests_ReturnsConfiguredValue()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 42, TimeSpan.FromMinutes(1));

        // Assert
        rateLimiter.MaxRequests.Should().Be(42);
    }

    [Fact]
    public void WindowDuration_ReturnsConfiguredValue()
    {
        // Arrange
        var expectedDuration = TimeSpan.FromSeconds(30);
        var rateLimiter = new RateLimiter(null, 10, expectedDuration);

        // Assert
        rateLimiter.WindowDuration.Should().Be(expectedDuration);
    }

    [Fact]
    public void DefaultConstructor_Uses60RequestsPerMinute()
    {
        // Arrange
        var rateLimiter = new RateLimiter();

        // Assert
        rateLimiter.MaxRequests.Should().Be(60);
        rateLimiter.WindowDuration.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SlidingWindow_CorrectlyExpireOldRequests()
    {
        // Arrange - 2 requests allowed per 100ms window
        var rateLimiter = new RateLimiter(null, 2, TimeSpan.FromMilliseconds(100));

        // Act
        await rateLimiter.TryAcquireAsync(default); // Request 1
        await rateLimiter.TryAcquireAsync(default); // Request 2
        bool atLimit = await rateLimiter.TryAcquireAsync(default); // Should fail

        atLimit.Should().BeFalse();

        // Wait for window to slide
        await Task.Delay(150);

        // Now should be able to make requests again
        bool afterExpiry = await rateLimiter.TryAcquireAsync(default);

        // Assert
        afterExpiry.Should().BeTrue();
    }
}
