// <copyright file="IPinService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>PIN hash + lockout (Cora pinStore parity).</summary>
public interface IPinService
{
    int FailedAttempts { get; }

    bool IsLockedOut { get; }

    TimeSpan? LockoutRemaining { get; }

    Task SetPinAsync(string pin);

    Task<bool> VerifyPinAsync(string pin);

    /// <summary>
    /// Replaces the stored PIN after verifying <paramref name="oldPin"/>; returns false (leaving the old
    /// PIN active) when verification fails or the gate is locked out. The custody blob is keyed by a device
    /// secret, so no re-seal is needed. Use: Low (user-initiated PIN change). Scope: secure-store PIN record.
    /// </summary>
    Task<bool> ChangePinAsync(string oldPin, string newPin);

    Task<bool> HasPinAsync();

    /// <summary>Loads lockout / fail counters from secure storage into in-memory fields.</summary>
    Task RefreshAsync();
}
