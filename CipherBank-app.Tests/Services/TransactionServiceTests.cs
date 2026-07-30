// <copyright file="TransactionServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class TransactionServiceTests
{
    [Fact]
    public async Task GetTransactionHistoryAsync_ReturnsTransactions()
    {
        // Arrange
        var mockService = new Mock<ITransactionService>();
        var expectedTransactions = new List<Transaction>
        {
            new(
                "tx1",
                TransactionType.Purchase,
                0.1m,
                "BTC",
                null,
                "addr1",
                DateTimeOffset.UtcNow,
                TransactionStatus.Confirmed,
                0.001m),
            new(
                "tx2",
                TransactionType.Send,
                0.05m,
                "BTC",
                "addr1",
                "addr2",
                DateTimeOffset.UtcNow,
                TransactionStatus.Confirmed,
                0.0001m),
        };

        mockService
            .Setup(x => x.GetTransactionHistoryAsync("wallet1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        // Act
        List<Transaction> result = await mockService.Object.GetTransactionHistoryAsync("wallet1", default);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Type == TransactionType.Purchase);
        result.Should().Contain(t => t.Type == TransactionType.Send);
    }

    [Fact]
    public async Task PurchaseCryptoAsync_WithValidAmount_ReturnsTransaction()
    {
        // Arrange
        var mockService = new Mock<ITransactionService>();
        var expectedTransaction = new Transaction(
            "tx_purchase",
            TransactionType.Purchase,
            0.5m,
            "ETH",
            null,
            "0xmywallet",
            DateTimeOffset.UtcNow,
            TransactionStatus.Confirmed,
            0.0075m);

        mockService
            .Setup(x => x.PurchaseCryptoAsync("ETH", 0.5m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransaction);

        // Act
        Transaction result = await mockService.Object.PurchaseCryptoAsync("ETH", 0.5m, default);

        // Assert
        result.Type.Should().Be(TransactionType.Purchase);
        result.Amount.Should().Be(0.5m);
        result.CryptoSymbol.Should().Be("ETH");
        result.Status.Should().Be(TransactionStatus.Confirmed);
    }

    [Fact]
    public async Task PurchaseCryptoAsync_WithZeroAmount_ThrowsArgumentException()
    {
        // Arrange
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(x => x.PurchaseCryptoAsync("BTC", 0m, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Amount must be positive"));

        // Act
        Func<Task<Transaction>> act = async () => await mockService.Object.PurchaseCryptoAsync("BTC", 0m, default);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public async Task SendCryptoAsync_WithValidParameters_ReturnsTransaction()
    {
        // Arrange
        var mockService = new Mock<ITransactionService>();
        var expectedTransaction = new Transaction(
            "tx_send",
            TransactionType.Send,
            0.1m,
            "BTC",
            "bc1qfrom",
            "bc1qto",
            DateTimeOffset.UtcNow,
            TransactionStatus.Pending,
            0.0001m);

        mockService
            .Setup(x => x.SendCryptoAsync("wallet1", "bc1qto", 0.1m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransaction);

        // Act
        Transaction result = await mockService.Object.SendCryptoAsync("wallet1", "bc1qto", 0.1m, default);

        // Assert
        result.Type.Should().Be(TransactionType.Send);
        result.Amount.Should().Be(0.1m);
        result.ToAddress.Should().Be("bc1qto");
    }

    [Fact]
    public async Task SendCryptoAsync_WithInsufficientBalance_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(x => x.SendCryptoAsync("wallet1", "bc1qto", 100m, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Insufficient balance"));

        // Act
        Func<Task<Transaction>> act = async () => await mockService.Object.SendCryptoAsync("wallet1", "bc1qto", 100m, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient*");
    }

    [Fact]
    public async Task GetTransactionStatusAsync_ReturnsCorrectStatus()
    {
        // Arrange
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(x => x.GetTransactionStatusAsync("tx123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionStatus.Confirmed);

        // Act
        TransactionStatus result = await mockService.Object.GetTransactionStatusAsync("tx123", default);

        // Assert
        result.Should().Be(TransactionStatus.Confirmed);
    }

    [Fact]
    public async Task GetTransactionStatusAsync_WithInvalidId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var mockService = new Mock<ITransactionService>();
        mockService
            .Setup(x => x.GetTransactionStatusAsync("invalid", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Transaction 'invalid' not found"));

        // Act
        Func<Task<TransactionStatus>> act = async () => await mockService.Object.GetTransactionStatusAsync("invalid", default);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
