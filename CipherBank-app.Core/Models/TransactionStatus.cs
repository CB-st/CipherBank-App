// <copyright file="TransactionStatus.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Models;

/// <summary>
/// Represents the current status of a cryptocurrency transaction.
/// </summary>
public enum TransactionStatus
{
    Pending,
    Confirmed,
    Failed,
    Cancelled,
}
