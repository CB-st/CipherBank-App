// <copyright file="TransactionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Models;

public class TransactionTests
{
    [Theory]
    [InlineData(TransactionType.Purchase, "Purchased")]
    [InlineData(TransactionType.Send, "Sent")]
    [InlineData(TransactionType.Receive, "Received")]
    [InlineData(TransactionType.Exchange, "Exchanged")]
    public void TypeDescription_ReturnsCorrectDescription(TransactionType type, string expected)
    {
        // Arrange
        Transaction transaction = CreateTransaction(type);

        // Act & Assert
        transaction.TypeDescription.Should().Be(expected);
    }

    [Theory]
    [InlineData(TransactionType.Send, true)]
    [InlineData(TransactionType.Purchase, true)]
    [InlineData(TransactionType.Receive, false)]
    [InlineData(TransactionType.Exchange, false)]
    public void IsOutgoing_ReturnsCorrectValue(TransactionType type, bool expected)
    {
        // Arrange
        Transaction transaction = CreateTransaction(type);

        // Act & Assert
        transaction.IsOutgoing.Should().Be(expected);
    }

    [Theory]
    [InlineData(TransactionStatus.Confirmed, true)]
    [InlineData(TransactionStatus.Pending, false)]
    [InlineData(TransactionStatus.Failed, false)]
    [InlineData(TransactionStatus.Cancelled, false)]
    public void IsComplete_ReturnsCorrectValue(TransactionStatus status, bool expected)
    {
        // Arrange
        Transaction transaction = CreateTransaction(status: status);

        // Act & Assert
        transaction.IsComplete.Should().Be(expected);
    }

    [Theory]
    [InlineData(TransactionStatus.Pending, true)]
    [InlineData(TransactionStatus.Confirmed, false)]
    [InlineData(TransactionStatus.Failed, false)]
    [InlineData(TransactionStatus.Cancelled, false)]
    public void IsPending_ReturnsCorrectValue(TransactionStatus status, bool expected)
    {
        // Arrange
        Transaction transaction = CreateTransaction(status: status);

        // Act & Assert
        transaction.IsPending.Should().Be(expected);
    }

    [Fact]
    public void FormattedAmount_ReturnsCorrectFormat()
    {
        // Arrange
        Transaction transaction = new Transaction(
            "tx123",
            TransactionType.Purchase,
            0.12345678m,
            "BTC",
            null,
            "bc1qtest",
            DateTimeOffset.UtcNow,
            TransactionStatus.Confirmed,
            0.001m);

        // Act
        string result = transaction.FormattedAmount;

        // Assert
        result.Should().Be("0.12345678 BTC");
    }

    [Fact]
    public void FormattedFee_ReturnsCorrectFormat()
    {
        // Arrange
        Transaction transaction = new Transaction(
            "tx123",
            TransactionType.Purchase,
            1m,
            "ETH",
            null,
            "0xtest",
            DateTimeOffset.UtcNow,
            TransactionStatus.Confirmed,
            0.00150000m);

        // Act
        string result = transaction.FormattedFee;

        // Assert
        result.Should().Be("0.00150000 ETH");
    }

    private static Transaction CreateTransaction(
        TransactionType type = TransactionType.Purchase,
        TransactionStatus status = TransactionStatus.Confirmed)
    {
        return new Transaction(
            "tx123",
            type,
            1.0m,
            "BTC",
            "from_address",
            "to_address",
            DateTimeOffset.UtcNow,
            status,
            0.001m);
    }
}
