// <copyright file="IPinService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>PIN hash + lockout (Cora pinStore parity).</summary>
public interface IPinService
{
    /// <summary>Gets the consecutive failed verification count since the last success.</summary>
    int FailedAttempts { get; }

    /// <summary>Gets a value indicating whether the PIN gate is currently locked out.</summary>
    bool IsLockedOut { get; }

    /// <summary>Gets the remaining lockout duration, or null when not locked out.</summary>
    TimeSpan? LockoutRemaining { get; }

    /// <summary>
    /// Stores a new PIN (policy-checked, salt+hash staged then promoted atomically). Rejects PINs
    /// below the minimum length before writing any state. Use: Low (onboarding / PIN change).
    /// Scope: secure-store PIN record.
    /// </summary>
    Task SetPinAsync(string pin);

    /// <summary>
    /// Verifies <paramref name="pin"/> against the stored record, updating fail counters and lockout;
    /// recovers a torn staged write before reading. Use: High (unlock / step-up). Scope: secure-store
    /// PIN record.
    /// </summary>
    Task<bool> VerifyPinAsync(string pin);

    /// <summary>
    /// Replaces the stored PIN after verifying <paramref name="oldPin"/>; returns false (leaving the old
    /// PIN active) when verification fails or the gate is locked out. The custody blob is keyed by a device
    /// secret, so no re-seal is needed. Use: Low (user-initiated PIN change). Scope: secure-store PIN record.
    /// </summary>
    Task<bool> ChangePinAsync(string oldPin, string newPin);

    /// <summary>
    /// True when a PIN record (or recoverable staged pair) exists.
    /// Use: Medium (boot routing). Scope: secure-store PIN record.
    /// </summary>
    Task<bool> HasPinAsync();

    /// <summary>Loads lockout / fail counters from secure storage into in-memory fields.</summary>
    Task RefreshAsync();
}
