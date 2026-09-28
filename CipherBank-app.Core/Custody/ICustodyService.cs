// <copyright file="ICustodyService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>On-device custody seal/unlock (Cora custody.ts parity).</summary>
public interface ICustodyService
{
    /// <summary>Raised when in-memory mnemonic session state is cleared (manual lock, idle expiry, unlock rollback).</summary>
    event EventHandler? Locked;

    /// <summary>Gets a value indicating whether a mnemonic session is currently unlocked in memory.</summary>
    bool IsUnlocked { get; }

    /// <summary>Gets the UTC expiry of the current unlock session, or null when locked.</summary>
    DateTimeOffset? SessionExpiresAt { get; }

    /// <summary>
    /// True when a sealed custody blob exists on this device.
    /// Use: High (boot routing Welcome vs Unlock). Scope: this device's custody record.
    /// </summary>
    Task<bool> HasSealedWalletAsync();

    /// <summary>True when a device secret exists so OS-auth unlock is possible.</summary>
    Task<bool> CanUnlockWithDeviceOwnerAsync();

    /// <summary>
    /// The only supported way to replace the unlock PIN: custody enforces the device-secret invariant
    /// before delegating to the PIN gate, so a legacy PIN-derived blob can never be orphaned by a
    /// hash-only PIN swap. Use: Low (user-initiated PIN change). Scope: this device's custody record.
    /// </summary>
    Task<CustodyPinChangeResult> ChangePinAsync(string oldPin, string newPin);

    /// <summary>
    /// Seals <paramref name="mnemonic"/> under <paramref name="pin"/> (PIN policy enforced by the PIN
    /// gate before any secret is written). Use: Low (onboarding / restore). Scope: this device's
    /// custody record.
    /// </summary>
    Task SealAsync(string mnemonic, string pin);

    /// <summary>
    /// Opens the sealed blob with <paramref name="pin"/> and starts an in-memory session; a failed
    /// verification routes through <see cref="Lock"/> so no prior session survives. Returns false on
    /// wrong PIN or lockout. Use: High (unlock screen). Scope: this device's custody record.
    /// </summary>
    Task<bool> UnlockAsync(string pin);

    /// <summary>Unlock using the stored device secret (call after successful OS biometrics).</summary>
    Task<bool> UnlockWithDeviceSecretAsync();

    /// <summary>
    /// Clears the in-memory mnemonic session and raises <see cref="Locked"/>.
    /// Use: High (idle lock / manual lock / unlock rollback). Scope: in-memory session only.
    /// </summary>
    void Lock();

    /// <summary>
    /// Returns the unlocked mnemonic, or null when locked or expired. Callers must not persist the
    /// value. Use: Low (backup export / signing). Scope: in-memory session only.
    /// </summary>
    string? ExportMnemonic();
}
