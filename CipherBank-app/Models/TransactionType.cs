namespace CipherBank_app.Models;

/// <summary>
/// Represents the type of cryptocurrency transaction.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Purchase of cryptocurrency with fiat currency.
    /// </summary>
    Purchase,

    /// <summary>
    /// Sending cryptocurrency to another wallet.
    /// </summary>
    Send,

    /// <summary>
    /// Receiving cryptocurrency from another wallet.
    /// </summary>
    Receive,

    /// <summary>
    /// Exchange between different cryptocurrencies.
    /// </summary>
    Exchange
}
