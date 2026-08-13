// <copyright file="PinChangeOutcome.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Result of one change-PIN attempt: machine-readable status plus a user-facing message.</summary>
public readonly record struct PinChangeOutcome(PinChangeStatus Status, string Message)
{
    /// <summary>True only for <see cref="PinChangeStatus.Success"/>. Use: High. Scope: caller branch.</summary>
    public bool Succeeded => Status == PinChangeStatus.Success;
}
