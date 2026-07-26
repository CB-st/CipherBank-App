// <copyright file="WalletServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class WalletServiceTests
{
    [Fact]
    public async Task GetWalletsAsync_ReturnsUserWallets()
    {
        // Arrange
        var mockService = new Mock<IWalletService>();
        var expectedWallets = new List<Wallet>
        {
            new("wallet1", "BTC", "Bitcoin", 0.5m, "bc1qtest1", DateTimeOffset.UtcNow),
            new("wallet2", "ETH", "Ethereum", 2.0m, "0xtest2", DateTimeOffset.UtcNow),
        };

        mockService
            .Setup(x => x.GetWalletsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedWallets);

        // Act
        var result = await mockService.Object.GetWalletsAsync(default);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(w => w.CryptoSymbol == "BTC");
        result.Should().Contain(w => w.CryptoSymbol == "ETH");
    }

    [Fact]
    public async Task GetWalletAsync_WithValidId_ReturnsWallet()
    {
        // Arrange
        var mockService = new Mock<IWalletService>();
        var expectedWallet = new Wallet(
            "wallet1", "BTC", "Bitcoin", 0.5m, "bc1qtest", DateTimeOffset.UtcNow);

        mockService
            .Setup(x => x.GetWalletAsync("wallet1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedWallet);

        // Act
        var result = await mockService.Object.GetWalletAsync("wallet1", default);

        // Assert
        result.Id.Should().Be("wallet1");
        result.CryptoSymbol.Should().Be("BTC");
        result.Balance.Should().Be(0.5m);
    }

    [Fact]
    public async Task GetWalletAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var mockService = new Mock<IWalletService>();
        mockService
            .Setup(x => x.GetWalletAsync("invalid", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Wallet 'invalid' not found"));

        // Act
        var act = async () => await mockService.Object.GetWalletAsync("invalid", default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetWalletBalanceAsync_ReturnsCorrectBalance()
    {
        // Arrange
        var mockService = new Mock<IWalletService>();
        mockService
            .Setup(x => x.GetWalletBalanceAsync("wallet1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.5m);

        // Act
        var result = await mockService.Object.GetWalletBalanceAsync("wallet1", default);

        // Assert
        result.Should().Be(1.5m);
    }

    [Fact]
    public async Task CreateWalletAsync_WithValidSymbol_ReturnsNewWallet()
    {
        // Arrange
        var mockService = new Mock<IWalletService>();
        var newWallet = new Wallet(
            "newWallet", "SOL", "Solana", 0m, "solAddress", DateTimeOffset.UtcNow);

        mockService
            .Setup(x => x.CreateWalletAsync("SOL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(newWallet);

        // Act
        var result = await mockService.Object.CreateWalletAsync("SOL", default);

        // Assert
        result.CryptoSymbol.Should().Be("SOL");
        result.Balance.Should().Be(0m);
        result.Address.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateWalletAsync_WhenWalletExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockService = new Mock<IWalletService>();
        mockService
            .Setup(x => x.CreateWalletAsync("BTC", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Wallet for BTC already exists"));

        // Act
        var act = async () => await mockService.Object.CreateWalletAsync("BTC", default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }
}
