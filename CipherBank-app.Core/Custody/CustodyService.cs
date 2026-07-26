// <copyright file="CustodyService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.Custody;

/// <summary>Why a custody-level PIN change ended the way it did.</summary>
public enum CustodyPinChangeResult
{
    /// <summary>The stored PIN was replaced; the sealed blob is untouched.</summary>
    Changed,

    /// <summary>The supplied current PIN failed verification.</summary>
    WrongPin,

    /// <summary>Too many failed attempts; the PIN gate is temporarily locked.</summary>
    LockedOut,

    /// <summary>
    /// No device secret exists, so the blob may still be a legacy PIN-derived seal that only
    /// <see cref="ICustodyService.UnlockAsync"/> can migrate. Changing the PIN now would orphan it.
    /// </summary>
    DeviceSecretMissing,
}

/// <summary>On-device custody seal/unlock (Cora custody.ts parity).</summary>
public interface ICustodyService
{
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

    bool IsUnlocked { get; }

    string? ExportMnemonic();

    DateTimeOffset? SessionExpiresAt { get; }
}

/// <inheritdoc />
public sealed class CustodyService : ICustodyService
{
    internal const string BlobKey = "cb_custody_blob";
    internal const string DeviceSecretKey = "cb_device_secret_v1";

    /// <summary>
    /// Staged device secret written before the blob rewrite so an interrupted migration
    /// can still open the new seal (or fall back to the legacy PIN seal).
    /// </summary>
    internal const string StagingDeviceSecretKey = "cb_device_secret_v1_staging";

    private static readonly TimeSpan SessionTtl = TimeSpan.FromMinutes(5);

    private readonly ISecureStore _store;
    private readonly IPinService _pin;
    private string? _mnemonic;
    private DateTimeOffset? _expires;

    public CustodyService(ISecureStore store, IPinService pin)
    {
        _store = store;
        _pin = pin;
    }

    public bool IsUnlocked
    {
        get
        {
            if (_mnemonic is null)
            {
                return false;
            }

            if (_expires is not DateTimeOffset expires || expires <= DateTimeOffset.UtcNow)
            {
                Lock();
                return false;
            }

            return true;
        }
    }

    public DateTimeOffset? SessionExpiresAt => _expires;

    public async Task<bool> HasSealedWalletAsync()
        => !string.IsNullOrEmpty(await _store.GetAsync(BlobKey).ConfigureAwait(false));

    public async Task<bool> CanUnlockWithDeviceOwnerAsync()
        => !string.IsNullOrEmpty(await ResolveDeviceSecretAsync().ConfigureAwait(false));

    /// <summary>
    /// Refuses the change unless a device secret exists, because <see cref="UnlockAsync"/> still accepts a
    /// legacy PIN-derived blob and only migrates it on a successful unlock — swapping the PIN hash first
    /// would leave that blob undecryptable. With the invariant satisfied the PIN is a pure logical gate, so
    /// the change is a hash swap and the blob is never re-sealed.
    /// Use: Low (user-initiated PIN change). Scope: this device's custody record.
    /// </summary>
    public async Task<CustodyPinChangeResult> ChangePinAsync(string oldPin, string newPin)
    {
        if (string.IsNullOrEmpty(await _store.GetAsync(DeviceSecretKey).ConfigureAwait(false)))
        {
            return CustodyPinChangeResult.DeviceSecretMissing;
        }

        bool changed = await _pin.ChangePinAsync(oldPin, newPin).ConfigureAwait(false);
        return changed ? CustodyPinChangeResult.Changed
            : _pin.IsLockedOut ? CustodyPinChangeResult.LockedOut
            : CustodyPinChangeResult.WrongPin;
    }

    public async Task SealAsync(string mnemonic, string pin)
    {
        string normalized = MnemonicHelper.Normalize(mnemonic);
        if (!MnemonicHelper.Validate(normalized))
        {
            throw new ArgumentException("Invalid mnemonic.", nameof(mnemonic));
        }

        await _pin.SetPinAsync(pin).ConfigureAwait(false);
        await PersistDeviceSecretSealAsync(normalized).ConfigureAwait(false);
        _mnemonic = normalized;
        _expires = DateTimeOffset.UtcNow.Add(SessionTtl);
    }

    public async Task<bool> UnlockAsync(string pin)
    {
        if (!await _pin.VerifyPinAsync(pin).ConfigureAwait(false))
        {
            return false;
        }

        string? blob = await _store.GetAsync(BlobKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(blob))
        {
            return false;
        }

        string? deviceSecret = await ResolveDeviceSecretAsync().ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(deviceSecret) && TryOpen(blob, deviceSecret, out string? opened) && opened is not null)
            {
                // PIN is a logical gate; AES key is the device secret.
                _mnemonic = opened;
                await PromoteStagedDeviceSecretAsync(deviceSecret).ConfigureAwait(false);
            }
            else if (TryOpen(blob, pin, out string? legacyOpened) && legacyOpened is not null)
            {
                // Legacy PIN-derived blob, or interrupted migration that left DeviceSecretKey
                // without rewriting the blob — recover via PIN then re-seal.
                _mnemonic = legacyOpened;
                await PersistDeviceSecretSealAsync(legacyOpened).ConfigureAwait(false);
            }
            else
            {
                _mnemonic = null;
                _expires = null;
                return false;
            }

            _expires = DateTimeOffset.UtcNow.Add(SessionTtl);
            return true;
        }
        catch
        {
            _mnemonic = null;
            _expires = null;
            return false;
        }
    }

    public async Task<bool> UnlockWithDeviceSecretAsync()
    {
        string? deviceSecret = await ResolveDeviceSecretAsync().ConfigureAwait(false);
        string? blob = await _store.GetAsync(BlobKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(deviceSecret) || string.IsNullOrEmpty(blob))
        {
            return false;
        }

        try
        {
            if (!TryOpen(blob, deviceSecret, out string? opened) || opened is null)
            {
                _mnemonic = null;
                _expires = null;
                return false;
            }

            _mnemonic = opened;
            await PromoteStagedDeviceSecretAsync(deviceSecret).ConfigureAwait(false);
            _expires = DateTimeOffset.UtcNow.Add(SessionTtl);
            return true;
        }
        catch
        {
            _mnemonic = null;
            _expires = null;
            return false;
        }
    }

    public void Lock()
    {
        _mnemonic = null;
        _expires = null;
    }

    public string? ExportMnemonic()
        => IsUnlocked ? _mnemonic : null;

    /// <summary>
    /// Persists a device-secret seal without orphaning the wallet if the process dies mid-write.
    /// Stages the secret, rewrites the blob, then promotes the secret and clears staging.
    /// Use: Low (seal / legacy migrate). Scope: this device's custody secure-store keys.
    /// </summary>
    private async Task PersistDeviceSecretSealAsync(string mnemonic)
    {
        string deviceSecret = CreateDeviceSecret();
        string sealedBlob = CryptoBox.Seal(mnemonic, deviceSecret);
        await _store.SetAsync(StagingDeviceSecretKey, deviceSecret).ConfigureAwait(false);
        await _store.SetAsync(BlobKey, sealedBlob).ConfigureAwait(false);
        await _store.SetAsync(DeviceSecretKey, deviceSecret).ConfigureAwait(false);
        await _store.RemoveAsync(StagingDeviceSecretKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Prefer the promoted device secret; fall back to a staged secret from an interrupted migrate.
    /// Use: High (every unlock). Scope: custody secure-store lookup.
    /// </summary>
    private async Task<string?> ResolveDeviceSecretAsync()
    {
        string? promoted = await _store.GetAsync(DeviceSecretKey).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(promoted))
        {
            return promoted;
        }

        string? staged = await _store.GetAsync(StagingDeviceSecretKey).ConfigureAwait(false);
        return string.IsNullOrEmpty(staged) ? null : staged;
    }

    /// <summary>
    /// Completes an interrupted migrate when unlock opened the blob with a staged secret.
    /// Use: Low (recovery). Scope: custody secure-store keys.
    /// </summary>
    private async Task PromoteStagedDeviceSecretAsync(string deviceSecret)
    {
        string? promoted = await _store.GetAsync(DeviceSecretKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(promoted))
        {
            await _store.SetAsync(DeviceSecretKey, deviceSecret).ConfigureAwait(false);
        }

        await _store.RemoveAsync(StagingDeviceSecretKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens a sealed blob without throwing so unlock can try the next key material.
    /// Use: High (unlock paths). Scope: single CryptoBox open attempt.
    /// </summary>
    private static bool TryOpen(string blob, string keyMaterial, out string? mnemonic)
    {
        try
        {
            mnemonic = CryptoBox.Open(blob, keyMaterial);
            return true;
        }
        catch
        {
            mnemonic = null;
            return false;
        }
    }

    private static string CreateDeviceSecret()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
