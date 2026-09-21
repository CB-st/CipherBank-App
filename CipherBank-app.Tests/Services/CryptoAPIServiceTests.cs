// <copyright file="CryptoAPIServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class CryptoApiServiceTests
{
    [Fact]
    public async Task GetCryptoPricesAsync_ReturnsListOfCryptos()
    {
        // Arrange
        Mock<ICryptoApiService> mockService = new() { CallBase = true };
        List<CryptoCurrency> expectedCryptos =
        [
            new("BTC", "Bitcoin", 50000m, 500m, 1.0m, 1000000000m, 50000000m, "url1"),
            new("ETH", "Ethereum", 3000m, 30m, 1.0m, 500000000m, 20000000m, "url2")
        ];

        mockService
            .Setup(x => x.GetCryptoPricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCryptos);

        // Act
        List<CryptoCurrency> result = await mockService.Object.GetCryptoPricesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Symbol == "BTC");
        result.Should().Contain(c => c.Symbol == "ETH");
    }

    [Fact]
    public async Task GetCryptoPriceAsync_WithValidSymbol_ReturnsCrypto()
    {
        // Arrange
        Mock<ICryptoApiService> mockService = new() { CallBase = true };
        CryptoCurrency expectedCrypto = new(
            "BTC", "Bitcoin", 50000m, 500m, 1.0m, 1000000000m, 50000000m, "url");

        mockService
            .Setup(x => x.GetCryptoPriceAsync("BTC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCrypto);

        // Act
        CryptoCurrency result = await mockService.Object.GetCryptoPriceAsync("BTC");

        // Assert
        result.Symbol.Value.Should().Be("BTC");
        result.Name.Should().Be("Bitcoin");
        result.CurrentPrice.Should().Be(50000m);
    }

    [Fact]
    public async Task GetCryptoPriceAsync_WithInvalidSymbol_ThrowsKeyNotFoundException()
    {
        // Arrange
        Mock<ICryptoApiService> mockService = new() { CallBase = true };
        mockService
            .Setup(x => x.GetCryptoPriceAsync("INVALID", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Cryptocurrency 'INVALID' not found"));

        // Act
        Func<Task<CryptoCurrency>> act = async () => await mockService.Object.GetCryptoPriceAsync("INVALID");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetPriceHistoryAsync_ReturnsHistoricalData()
    {
        // Arrange
        Mock<ICryptoApiService> mockService = new() { CallBase = true };
        List<PricePoint> pricePoints =
        [
            new(DateTimeOffset.UtcNow.AddDays(-1), 49000m),
            new(DateTimeOffset.UtcNow, 50000m)
        ];
        PriceHistory expectedHistory = new(
            "BTC",
            pricePoints,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        mockService
            .Setup(x => x.GetPriceHistoryAsync("BTC", "1d", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHistory);

        // Act
        PriceHistory result = await mockService.Object.GetPriceHistoryAsync("BTC", "1d");

        // Assert
        result.Symbol.Value.Should().Be("BTC");
        result.PricePoints.Should().HaveCount(2);
        result.PriceChange.Should().Be(1000m);
    }

    [Fact]
    public async Task SearchCryptoAsync_WithMatchingQuery_ReturnsResults()
    {
        // Arrange
        Mock<ICryptoApiService> mockService = new() { CallBase = true };
        List<CryptoCurrency> expectedResults =
        [
            new("BTC", "Bitcoin", 50000m, 500m, 1.0m, 1000000000m, 50000000m, "url")
        ];

        mockService
            .Setup(x => x.SearchCryptoAsync("bit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        // Act
        List<CryptoCurrency> result = await mockService.Object.SearchCryptoAsync("bit");

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Contain("Bitcoin");
    }

    [Fact]
    public async Task SearchCryptoAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        Mock<ICryptoApiService> mockService = new() { CallBase = true };
        mockService
            .Setup(x => x.SearchCryptoAsync("xyz123", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        List<CryptoCurrency> result = await mockService.Object.SearchCryptoAsync("xyz123");

        // Assert
        result.Should().BeEmpty();
    }
}
