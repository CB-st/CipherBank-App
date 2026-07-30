// <copyright file="CryptoAPIServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class CryptoAPIServiceTests
{
    [Fact]
    public async Task GetCryptoPricesAsync_ReturnsListOfCryptos()
    {
        // Arrange
        var mockService = new Mock<ICryptoApiService>();
        var expectedCryptos = new List<CryptoCurrency>
        {
            new("BTC", "Bitcoin", 50000m, 500m, 1.0m, 1000000000m, 50000000m, null),
            new("ETH", "Ethereum", 3000m, 30m, 1.0m, 500000000m, 20000000m, null),
        };

        mockService
            .Setup(x => x.GetCryptoPricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCryptos);

        // Act
        List<CryptoCurrency> result = await mockService.Object.GetCryptoPricesAsync(default);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Symbol == "BTC");
        result.Should().Contain(c => c.Symbol == "ETH");
    }

    [Fact]
    public async Task GetCryptoPriceAsync_WithValidSymbol_ReturnsCrypto()
    {
        // Arrange
        var mockService = new Mock<ICryptoApiService>();
        var expectedCrypto = new CryptoCurrency(
            "BTC", "Bitcoin", 50000m, 500m, 1.0m, 1000000000m, 50000000m, new Uri("https://example.com/btc.png"));

        mockService
            .Setup(x => x.GetCryptoPriceAsync("BTC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCrypto);

        // Act
        CryptoCurrency result = await mockService.Object.GetCryptoPriceAsync("BTC", default);

        // Assert
        result.Symbol.Should().Be("BTC");
        result.Name.Should().Be("Bitcoin");
        result.CurrentPrice.Should().Be(50000m);
    }

    [Fact]
    public async Task GetCryptoPriceAsync_WithInvalidSymbol_ThrowsKeyNotFoundException()
    {
        // Arrange
        var mockService = new Mock<ICryptoApiService>();
        mockService
            .Setup(x => x.GetCryptoPriceAsync("INVALID", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Cryptocurrency 'INVALID' not found"));

        // Act
        Func<Task<CryptoCurrency>> act = async () => await mockService.Object.GetCryptoPriceAsync("INVALID", default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetPriceHistoryAsync_ReturnsHistoricalData()
    {
        // Arrange
        var mockService = new Mock<ICryptoApiService>();
        var pricePoints = new List<PricePoint>
        {
            new(DateTimeOffset.UtcNow.AddDays(-1), 49000m),
            new(DateTimeOffset.UtcNow, 50000m),
        };
        var expectedHistory = new PriceHistory(
            "BTC",
            pricePoints,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        mockService
            .Setup(x => x.GetPriceHistoryAsync("BTC", "1d", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        PriceHistory result = await mockService.Object.GetPriceHistoryAsync("BTC", "1d", default);

        // Assert
        result.Symbol.Should().Be("BTC");
        result.PricePoints.Should().HaveCount(2);
        result.PriceChange.Should().Be(1000m);
    }

    [Fact]
    public async Task SearchCryptoAsync_WithMatchingQuery_ReturnsResults()
    {
        // Arrange
        var mockService = new Mock<ICryptoApiService>();
        var expectedResults = new List<CryptoCurrency>
        {
            new("BTC", "Bitcoin", 50000m, 500m, 1.0m, 1000000000m, 50000000m, null),
        };

        mockService
            .Setup(x => x.SearchCryptoAsync("bit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        List<CryptoCurrency> result = await mockService.Object.SearchCryptoAsync("bit", default);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Contain("Bitcoin");
    }

    [Fact]
    public async Task SearchCryptoAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var mockService = new Mock<ICryptoApiService>();
        mockService
            .Setup(x => x.SearchCryptoAsync("xyz123", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        List<CryptoCurrency> result = await mockService.Object.SearchCryptoAsync("xyz123", default);

        // Assert
        result.Should().BeEmpty();
    }
}
