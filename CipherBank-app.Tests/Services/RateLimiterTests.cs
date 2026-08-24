// <copyright file="RateLimiterTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

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
        var result = await rateLimiter.TryAcquireAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_AtLimit_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 3, TimeSpan.FromMinutes(1));

        // Act - Make 3 requests (the limit)
        await rateLimiter.TryAcquireAsync();
        await rateLimiter.TryAcquireAsync();
        await rateLimiter.TryAcquireAsync();

        // 4th request should fail
        var result = await rateLimiter.TryAcquireAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_AfterWindowExpires_ReturnsTrue()
    {
        // Arrange - Very short window
        var rateLimiter = new RateLimiter(null, 1, TimeSpan.FromMilliseconds(50));

        // Act - Make request, wait for window, make another
        await rateLimiter.TryAcquireAsync();
        await Task.Delay(100); // Wait for window to expire
        var result = await rateLimiter.TryAcquireAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetWaitTimeAsync_WhenUnderLimit_ReturnsZero()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 10, TimeSpan.FromMinutes(1));

        // Act
        var waitTime = await rateLimiter.GetWaitTimeAsync();

        // Assert
        waitTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetWaitTimeAsync_WhenAtLimit_ReturnsPositive()
    {
        // Arrange
        var rateLimiter = new RateLimiter(null, 1, TimeSpan.FromSeconds(10));

        // Act
        await rateLimiter.TryAcquireAsync(); // Use up the limit
        var waitTime = await rateLimiter.GetWaitTimeAsync();

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
        await rateLimiter.TryAcquireAsync();
        await rateLimiter.TryAcquireAsync();
        await rateLimiter.TryAcquireAsync();

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
        await rateLimiter.TryAcquireAsync(); // Request 1
        await rateLimiter.TryAcquireAsync(); // Request 2
        var atLimit = await rateLimiter.TryAcquireAsync(); // Should fail

        atLimit.Should().BeFalse();

        // Wait for window to slide
        await Task.Delay(150);

        // Now should be able to make requests again
        var afterExpiry = await rateLimiter.TryAcquireAsync();

        // Assert
        afterExpiry.Should().BeTrue();
    }
}
