// <copyright file="IAppSession.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Session;

/// <summary>App-level session: custody unlock + product tokens + idle lock.</summary>
public interface IAppSession
{
    /// <summary>Raised when the session locks (idle expiry, manual lock, unlock rollback).</summary>
    event EventHandler? Locked;

    /// <summary>Gets a value indicating whether <see cref="BootAsync"/> is still running.</summary>
    bool IsBooting { get; }

    /// <summary>Gets a value indicating whether a sealed custody record exists on this device.</summary>
    bool HasWallet { get; }

    /// <summary>Gets a value indicating whether custody is unlocked and the session is live.</summary>
    bool IsUnlocked { get; }

    /// <summary>Gets or sets the idle-lock threshold in milliseconds.</summary>
    int IdleMs { get; set; }

    /// <summary>Gets the current product access token, or null when no product session exists.</summary>
    string? AccessToken { get; }

    /// <summary>
    /// Initializes persistence and custody state on app start; idempotent.
    /// Use: High (app start). Scope: process-wide session.
    /// </summary>
    Task BootAsync();

    /// <summary>
    /// Unlocks custody with <paramref name="pin"/> and completes session start (prefs, product
    /// session). Returns false on wrong PIN or lockout.
    /// Use: High (unlock screen). Scope: process-wide session.
    /// </summary>
    Task<bool> UnlockAsync(string pin);

    /// <summary>Unlock after successful OS biometrics (device-secret path).</summary>
    Task<bool> UnlockWithDeviceOwnerAsync();

    /// <summary>True when a device secret exists so the biometric unlock path can be offered.</summary>
    Task<bool> CanUnlockWithDeviceOwnerAsync();

    /// <summary>Records user activity so the idle-lock window restarts. Use: High (every interaction).</summary>
    void Touch();

    /// <summary>
    /// Locks custody, clears product tokens, and raises <see cref="Locked"/>.
    /// Use: High (idle / manual lock). Scope: process-wide session.
    /// </summary>
    void Lock();

    /// <summary>
    /// Seals a new wallet then finishes session start in one gated step (onboarding / restore).
    /// Use: Low (onboarding). Scope: process-wide session.
    /// </summary>
    Task FinishCustodySetupAsync(string mnemonic, string pin);

    /// <summary>Returns true if idle exceeded and lock was applied.</summary>
    bool CheckIdleAndMaybeLock();
}
