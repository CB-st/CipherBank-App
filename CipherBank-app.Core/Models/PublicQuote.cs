// <copyright file="PublicQuote.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Models;

/// <summary>
/// Indicative quote from the CipherBank public <c>/quote</c> or <c>/iquote</c> API.
/// Not a server-honored lock until <c>/quote/lock</c> exists.
/// </summary>
public sealed record PublicQuote(
    string InputCurrency,
    decimal InputAmount,
    string OutputCurrency,
    decimal OutputAmount)
{
    /// <summary>
    /// Effective output-per-input rate when input amount is positive.
    /// </summary>
    public decimal Rate => InputAmount == 0m ? 0m : OutputAmount / InputAmount;
}
