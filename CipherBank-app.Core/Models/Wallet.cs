// <copyright file="Wallet.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Models;

/// <summary>
/// Represents a cryptocurrency wallet belonging to the user.
/// </summary>
public record Wallet(
    string Id,
    string CryptoSymbol,
    string CryptoName,
    decimal Balance,
    string Address,
    DateTimeOffset CreatedAt)
{
    public string FormattedBalance => $"{Balance:F8} {CryptoSymbol}";

    public bool HasBalance => Balance > 0;
}
