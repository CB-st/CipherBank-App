// <copyright file="TransactionStatus.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
