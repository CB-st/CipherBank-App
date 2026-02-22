using System;

namespace CipherBank_app.Models;

/// <summary>
/// Represents a cryptocurrency wallet belonging to the user.
/// </summary>
/// <param name="Id">Unique identifier for the wallet.</param>
/// <param name="CryptoSymbol">The cryptocurrency symbol (e.g., BTC, ETH).</param>
/// <param name="CryptoName">The full name of the cryptocurrency.</param>
/// <param name="Balance">The current balance in the wallet.</param>
/// <param name="Address">The public blockchain address for the wallet.</param>
/// <param name="CreatedAt">When the wallet was created.</param>
public record Wallet(
    string Id,
    string CryptoSymbol,
    string CryptoName,
    decimal Balance,
    string Address,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Gets the formatted balance string.
    /// </summary>
    public string FormattedBalance => $"{Balance:F8} {CryptoSymbol}";

    /// <summary>
    /// Gets whether the wallet has any balance.
    /// </summary>
    public bool HasBalance => Balance > 0;
}
