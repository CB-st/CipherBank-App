namespace CipherBank_app.Models;

/// <summary>
/// Represents the current status of a cryptocurrency transaction.
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending confirmation on the blockchain.
    /// </summary>
    Pending,

    /// <summary>
    /// Transaction has been confirmed and completed.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Transaction failed due to an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Transaction was cancelled by the user or system.
    /// </summary>
    Cancelled
}
