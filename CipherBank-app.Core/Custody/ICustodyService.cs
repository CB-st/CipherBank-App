// <copyright file="ICustodyService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>On-device custody seal/unlock (Cora custody.ts parity).</summary>
public interface ICustodyService
{
    /// <summary>Raised when in-memory mnemonic session state is cleared (manual lock, idle expiry, unlock rollback).</summary>
    event EventHandler? Locked;

    bool IsUnlocked { get; }

    DateTimeOffset? SessionExpiresAt { get; }

    Task<bool> HasSealedWalletAsync();

    /// <summary>True when a device secret exists so OS-auth unlock is possible.</summary>
    Task<bool> CanUnlockWithDeviceOwnerAsync();

    /// <summary>
    /// The only supported way to replace the unlock PIN: custody enforces the device-secret invariant
    /// before delegating to the PIN gate, so a legacy PIN-derived blob can never be orphaned by a
    /// hash-only PIN swap. Use: Low (user-initiated PIN change). Scope: this device's custody record.
    /// </summary>
    Task<CustodyPinChangeResult> ChangePinAsync(string oldPin, string newPin);

    Task SealAsync(string mnemonic, string pin);

    Task<bool> UnlockAsync(string pin);

    /// <summary>Unlock using the stored device secret (call after successful OS biometrics).</summary>
    Task<bool> UnlockWithDeviceSecretAsync();

    void Lock();

    string? ExportMnemonic();
}
