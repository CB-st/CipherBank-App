// <copyright file="CustodyService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.Custody;

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

    private const int DeviceSecretByteLength = 32;

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
        return MapPinChangeResult(changed);
    }

    public Task SealAsync(string mnemonic, string pin)
    {
        string normalized = MnemonicHelper.Normalize(mnemonic);
        if (!MnemonicHelper.Validate(normalized))
        {
            throw new ArgumentException("Invalid mnemonic.", nameof(mnemonic));
        }

        return SealValidatedAsync(normalized, pin);
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
    /// Maps a PIN-service swap outcome to custody status without nested ternaries.
    /// Use: Low (PIN change). Scope: ChangePinAsync.
    /// </summary>
    private CustodyPinChangeResult MapPinChangeResult(bool changed)
    {
        if (changed)
        {
            return CustodyPinChangeResult.Changed;
        }

        if (_pin.IsLockedOut)
        {
            return CustodyPinChangeResult.LockedOut;
        }

        return CustodyPinChangeResult.WrongPin;
    }

    /// <summary>
    /// Persists seal after mnemonic validation; caller must have normalized and validated first.
    /// Use: Low (seal). Scope: SealAsync.
    /// </summary>
    private async Task SealValidatedAsync(string normalized, string pin)
    {
        await _pin.SetPinAsync(pin).ConfigureAwait(false);
        await PersistDeviceSecretSealAsync(normalized).ConfigureAwait(false);
        _mnemonic = normalized;
        // Sonar S6354: TimeProvider/IClock injection deferred (docs/SONAR_GATE.md).
        _expires = DateTimeOffset.UtcNow.Add(SessionTtl); // NOSONAR (S6354)
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
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(DeviceSecretByteLength));

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
}
