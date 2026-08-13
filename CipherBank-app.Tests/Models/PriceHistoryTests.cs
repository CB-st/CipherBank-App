// <copyright file="PriceHistoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Models;

public class PriceHistoryTests
{
    [Fact]
    public void HighPrice_ReturnsMaximumPrice()
    {
        // Arrange
        var pricePoints = new List<PricePoint>
        {
            new(DateTimeOffset.UtcNow.AddHours(-3), 100m),
            new(DateTimeOffset.UtcNow.AddHours(-2), 150m),
            new(DateTimeOffset.UtcNow.AddHours(-1), 120m),
        };
        var history = CreatePriceHistory(pricePoints);

        // Act & Assert
        history.HighPrice.Should().Be(150m);
    }

    [Fact]
    public void LowPrice_ReturnsMinimumPrice()
    {
        // Arrange
        var pricePoints = new List<PricePoint>
        {
            new(DateTimeOffset.UtcNow.AddHours(-3), 100m),
            new(DateTimeOffset.UtcNow.AddHours(-2), 150m),
            new(DateTimeOffset.UtcNow.AddHours(-1), 120m),
        };
        var history = CreatePriceHistory(pricePoints);

        // Act & Assert
        history.LowPrice.Should().Be(100m);
    }

    [Fact]
    public void AveragePrice_ReturnsCorrectAverage()
    {
        // Arrange
        var pricePoints = new List<PricePoint>
        {
            new(DateTimeOffset.UtcNow.AddHours(-3), 100m),
            new(DateTimeOffset.UtcNow.AddHours(-2), 150m),
            new(DateTimeOffset.UtcNow.AddHours(-1), 200m),
        };
        var history = CreatePriceHistory(pricePoints);

        // Act & Assert
        history.AveragePrice.Should().Be(150m);
    }

    [Fact]
    public void PriceChange_ReturnsLastMinusFirst()
    {
        // Arrange
        var pricePoints = new List<PricePoint>
        {
            new(DateTimeOffset.UtcNow.AddHours(-3), 100m),
            new(DateTimeOffset.UtcNow.AddHours(-2), 150m),
            new(DateTimeOffset.UtcNow.AddHours(-1), 120m),
        };
        var history = CreatePriceHistory(pricePoints);

        // Act & Assert
        history.PriceChange.Should().Be(20m); // 120 - 100
    }

    [Fact]
    public void PercentChange_ReturnsCorrectPercentage()
    {
        // Arrange
        var pricePoints = new List<PricePoint>
        {
            new(DateTimeOffset.UtcNow.AddHours(-2), 100m),
            new(DateTimeOffset.UtcNow.AddHours(-1), 120m),
        };
        var history = CreatePriceHistory(pricePoints);

        // Act & Assert
        history.PercentChange.Should().Be(20m); // (20 / 100) * 100
    }

    [Fact]
    public void EmptyPricePoints_ReturnsZeroForAllCalculations()
    {
        // Arrange
        var history = CreatePriceHistory([]);

        // Act & Assert
        history.HighPrice.Should().Be(0);
        history.LowPrice.Should().Be(0);
        history.AveragePrice.Should().Be(0);
        history.PriceChange.Should().Be(0);
        history.PercentChange.Should().Be(0);
    }

    [Fact]
    public void SinglePricePoint_ReturnsZeroForChange()
    {
        // Arrange
        var pricePoints = new List<PricePoint>
        {
            new(DateTimeOffset.UtcNow, 100m),
        };
        var history = CreatePriceHistory(pricePoints);

        // Act & Assert
        history.HighPrice.Should().Be(100m);
        history.LowPrice.Should().Be(100m);
        history.AveragePrice.Should().Be(100m);
        history.PriceChange.Should().Be(0);
    }

    private static PriceHistory CreatePriceHistory(List<PricePoint> pricePoints)
    {
        return new PriceHistory(
            "BTC",
            pricePoints,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);
    }
}
