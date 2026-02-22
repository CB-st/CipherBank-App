using System;
using System.Text.Json.Serialization;

namespace CipherBank_app.Models;

/// <summary>
/// Represents a cryptocurrency transaction.
/// </summary>
/// <param name="Id">Unique transaction identifier.</param>
/// <param name="Type">The type of transaction.</param>
/// <param name="Amount">The amount of cryptocurrency transacted.</param>
/// <param name="CryptoSymbol">The cryptocurrency symbol.</param>
/// <param name="FromAddress">The source wallet address (null for purchases).</param>
/// <param name="ToAddress">The destination wallet address.</param>
/// <param name="Timestamp">When the transaction occurred.</param>
/// <param name="Status">The current status of the transaction.</param>
/// <param name="FeeAmount">The transaction fee amount.</param>
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
    /// <summary>
    /// Gets the formatted amount string.
    /// </summary>
    public string FormattedAmount => $"{Amount:F8} {CryptoSymbol}";

    /// <summary>
    /// Gets the formatted fee string.
    /// </summary>
    public string FormattedFee => $"{FeeAmount:F8} {CryptoSymbol}";

    /// <summary>
    /// Gets a user-friendly description of the transaction type.
    /// </summary>
    public string TypeDescription => Type switch
    {
        TransactionType.Purchase => "Purchased",
        TransactionType.Send => "Sent",
        TransactionType.Receive => "Received",
        TransactionType.Exchange => "Exchanged",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets whether this is an outgoing transaction.
    /// </summary>
    public bool IsOutgoing => Type is TransactionType.Send or TransactionType.Purchase;

    /// <summary>
    /// Gets whether the transaction is complete.
    /// </summary>
    public bool IsComplete => Status == TransactionStatus.Confirmed;

    /// <summary>
    /// Gets whether the transaction is pending.
    /// </summary>
    public bool IsPending => Status == TransactionStatus.Pending;
}
