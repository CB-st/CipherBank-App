// <copyright file="TransactionType.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Models;

/// <summary>
/// Represents the type of cryptocurrency transaction.
/// </summary>
public enum TransactionType
{
    Purchase,
    Send,
    Receive,
    Exchange,
}
