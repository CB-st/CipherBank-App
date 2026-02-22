// <copyright file="Transaction.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.Models;

/// <summary>
/// Represents a cryptocurrency transaction.
/// </summary>
public record Transaction(
    string Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    TransactionType Type,
    decimal Amount,
    string CryptoSymbol,
    string? FromAddress,
    string? ToAddress,
    DateTimeOffset Timestamp,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    TransactionStatus Status,
    decimal FeeAmount)
{
    public string FormattedAmount => $"{Amount:F8} {CryptoSymbol}";

    public string FormattedFee => $"{FeeAmount:F8} {CryptoSymbol}";

    public string TypeDescription => Type switch
    {
        TransactionType.Purchase => "Purchased",
        TransactionType.Send => "Sent",
        TransactionType.Receive => "Received",
        TransactionType.Exchange => "Exchanged",
        _ => "Unknown",
    };

    public bool IsOutgoing => Type is TransactionType.Send or TransactionType.Purchase;

    public bool IsComplete => Status == TransactionStatus.Confirmed;

    public bool IsPending => Status == TransactionStatus.Pending;
}
