// <copyright file="AchRecipientField.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Identifies the ACH recipient form field an <see cref="AchValidationError"/> belongs to.
/// Order mirrors the stable validation order: name, holder, bank, routing, account,
/// account type, memo.
/// </summary>
public enum AchRecipientField
{
    /// <summary>Payee display name.</summary>
    Name,

    /// <summary>Account holder name.</summary>
    Holder,

    /// <summary>Bank name.</summary>
    Bank,

    /// <summary>ABA routing number (nine ASCII digits).</summary>
    Routing,

    /// <summary>Account number.</summary>
    Account,

    /// <summary>Account type (checking or savings).</summary>
    AccountType,

    /// <summary>Optional memo.</summary>
    Memo,
}
