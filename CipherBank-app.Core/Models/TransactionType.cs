// <copyright file="TransactionType.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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
