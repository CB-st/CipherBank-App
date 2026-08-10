// <copyright file="CustodyService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.Configuration;

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
    private readonly ICryptoBox _cryptoBox;
    private readonly TimeProvider _timeProvider;
    private string? _mnemonic;
    private DateTimeOffset? _expires;

    public CustodyService(ISecureStore store, IPinService pin)
        : this(store, pin, new AesGcmCryptoBox(CryptographyOptions.Default), TimeProvider.System)
    {
    }

    public CustodyService(ISecureStore store, IPinService pin, TimeProvider timeProvider)
        : this(store, pin, new AesGcmCryptoBox(CryptographyOptions.Default), timeProvider)
    {
    }

    public CustodyService(ISecureStore store, IPinService pin, ICryptoBox cryptoBox)
        : this(store, pin, cryptoBox, TimeProvider.System)
    {
    }

    public CustodyService(
        ISecureStore store,
        IPinService pin,
        ICryptoBox cryptoBox,
        TimeProvider timeProvider)
    {
        _store = store;
        _pin = pin;
        _cryptoBox = cryptoBox;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool IsUnlocked
    {
        get
        {
            if (_mnemonic is null)
            {
                return false;
            }

            if (_expires is not DateTimeOffset expires || expires <= _timeProvider.GetUtcNow())
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

        var changed = await _pin.ChangePinAsync(oldPin, newPin).ConfigureAwait(false);
        return MapPinChangeResult(changed);
    }

    public Task SealAsync(string mnemonic, string pin)
    {
        var normalized = MnemonicHelper.Normalize(mnemonic);
        if (!MnemonicHelper.Validate(normalized))
        {
            throw new ArgumentException("Invalid mnemonic.", nameof(mnemonic));
        }

        return SealValidatedAsync(normalized, pin);
    }

    /// <summary>
    /// Unlocks custody with PIN, preferring device-secret seal then legacy PIN seal.
    /// Use: High (every PIN unlock). Scope: CustodyService session.
    /// </summary>
    public async Task<bool> UnlockAsync(string pin)
    {
        if (!await _pin.VerifyPinAsync(pin).ConfigureAwait(false))
        {
            return false;
        }

        var blob = await _store.GetAsync(BlobKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(blob))
        {
            return false;
        }

        try
        {
            if (!await TryUnlockWithDeviceSecretsAsync(blob).ConfigureAwait(false))
            {
                if (!TryOpen(blob, pin, out var legacyOpened) || legacyOpened is null)
                {
                    _mnemonic = null;
                    _expires = null;
                    return false;
                }

                // Legacy PIN-derived blob, or interrupted migration that left DeviceSecretKey
                // without rewriting the blob — recover via PIN then re-seal.
                _mnemonic = legacyOpened;
                await PersistDeviceSecretSealAsync(legacyOpened).ConfigureAwait(false);
            }

            _expires = _timeProvider.GetUtcNow().Add(SessionTtl);
            return true;
        }
        catch (CryptographicException)
        {
            return FailUnlock();
        }
        catch (FormatException)
        {
            return FailUnlock();
        }
        catch (InvalidOperationException)
        {
            return FailUnlock();
        }
        catch (ArgumentException)
        {
            return FailUnlock();
        }
    }

    /// <summary>
    /// Unlocks custody using the persisted device secret without a PIN prompt.
    /// Use: Medium (biometric / auto-unlock). Scope: CustodyService session.
    /// </summary>
    public async Task<bool> UnlockWithDeviceSecretAsync()
    {
        var blob = await _store.GetAsync(BlobKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(blob))
        {
            return false;
        }

        try
        {
            if (!await TryUnlockWithDeviceSecretsAsync(blob).ConfigureAwait(false))
            {
                _mnemonic = null;
                _expires = null;
                return false;
            }

            _expires = _timeProvider.GetUtcNow().Add(SessionTtl);
            return true;
        }
        catch (CryptographicException)
        {
            return FailUnlock();
        }
        catch (FormatException)
        {
            return FailUnlock();
        }
        catch (InvalidOperationException)
        {
            return FailUnlock();
        }
        catch (ArgumentException)
        {
            return FailUnlock();
        }
    }

    public void Lock()
    {
        _mnemonic = null;
        _expires = null;
    }

    public string? ExportMnemonic()
        => IsUnlocked ? _mnemonic : null;

    private static string CreateDeviceSecret()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(DeviceSecretByteLength));

    /// <summary>
    /// Opens a sealed blob without throwing so unlock can try the next key material.
    /// Use: High (unlock paths). Scope: single CryptoBox open attempt.
    /// </summary>
    private bool TryOpen(string blob, string keyMaterial, out string? mnemonic)
    {
        try
        {
            mnemonic = _cryptoBox.Open(blob, keyMaterial);
            return true;
        }
        catch (CryptographicException)
        {
            mnemonic = null;
            return false;
        }
        catch (FormatException)
        {
            mnemonic = null;
            return false;
        }
        catch (InvalidOperationException)
        {
            mnemonic = null;
            return false;
        }
        catch (ArgumentException)
        {
            mnemonic = null;
            return false;
        }
    }

    /// <summary>
    /// Clears in-memory custody session state after a failed unlock attempt.
    /// Use: Medium (unlock failure paths). Scope: CustodyService session fields.
    /// </summary>
    private bool FailUnlock()
    {
        _mnemonic = null;
        _expires = null;
        return false;
    }

    /// <summary>
    /// Maps PIN-change success / lockout / wrong-PIN into <see cref="CustodyPinChangeResult"/>.
    /// Use: Medium (ChangePinAsync). Scope: this service.
    /// </summary>
    private CustodyPinChangeResult MapPinChangeResult(bool changed)
    {
        if (changed)
        {
            return CustodyPinChangeResult.Changed;
        }

        return _pin.IsLockedOut ? CustodyPinChangeResult.LockedOut : CustodyPinChangeResult.WrongPin;
    }

    /// <summary>
    /// Persists PIN + sealed mnemonic after shape validation.
    /// Use: Medium (SealAsync). Scope: this service.
    /// </summary>
    private async Task SealValidatedAsync(string normalizedMnemonic, string pin)
    {
        await _pin.SetPinAsync(pin).ConfigureAwait(false);
        await PersistDeviceSecretSealAsync(normalizedMnemonic).ConfigureAwait(false);
        _mnemonic = normalizedMnemonic;

        // Sonar S6354: TimeProvider/IClock injection deferred (docs/SONAR_GATE.md).
        _expires = _timeProvider.GetUtcNow().Add(SessionTtl);
    }

    /// <summary>
    /// Persists a device-secret seal without orphaning the wallet if the process dies mid-write.
    /// Stages the secret, rewrites the blob, then promotes the secret and clears staging.
    /// Use: Low (seal / legacy migrate). Scope: this device's custody secure-store keys.
    /// </summary>
    private async Task PersistDeviceSecretSealAsync(string mnemonic)
    {
        var deviceSecret = CreateDeviceSecret();
        var sealedBlob = _cryptoBox.Seal(mnemonic, deviceSecret);
        await _store.SetAsync(StagingDeviceSecretKey, deviceSecret).ConfigureAwait(false);
        await _store.SetAsync(BlobKey, sealedBlob).ConfigureAwait(false);
        await _store.SetAsync(DeviceSecretKey, deviceSecret).ConfigureAwait(false);
        await _store.RemoveAsync(StagingDeviceSecretKey).ConfigureAwait(false);
    }

    /// <summary>
    /// Prefer the promoted device secret; fall back to a staged secret from an interrupted migrate.
    /// Use: High (capability checks). Scope: custody secure-store lookup.
    /// </summary>
    private async Task<string?> ResolveDeviceSecretAsync()
    {
        var promoted = await _store.GetAsync(DeviceSecretKey).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(promoted))
        {
            return promoted;
        }

        var staged = await _store.GetAsync(StagingDeviceSecretKey).ConfigureAwait(false);
        return string.IsNullOrEmpty(staged) ? null : staged;
    }

    /// <summary>
    /// Opens the seal with the promoted secret, then the staged secret if the promoted key
    /// cannot decrypt (interrupted reseal left a new blob + staged key while the old promoted
    /// secret remained). Commits whichever secret successfully opened the blob.
    /// Use: High (every unlock). Scope: custody secure-store keys + in-memory mnemonic.
    /// </summary>
    private async Task<bool> TryUnlockWithDeviceSecretsAsync(string blob)
    {
        var promoted = await _store.GetAsync(DeviceSecretKey).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(promoted) && TryOpen(blob, promoted, out var opened) && opened is not null)
        {
            _mnemonic = opened;
            await CommitWorkingDeviceSecretAsync(promoted).ConfigureAwait(false);
            return true;
        }

        var staged = await _store.GetAsync(StagingDeviceSecretKey).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(staged) && TryOpen(blob, staged, out var stagedOpened) && stagedOpened is not null)
        {
            _mnemonic = stagedOpened;
            await CommitWorkingDeviceSecretAsync(staged).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Persists the secret that successfully opened the blob and clears staging.
    /// Overwrites a stale promoted secret after an interrupted reseal recovery.
    /// Use: Low (recovery). Scope: custody secure-store keys.
    /// </summary>
    private async Task CommitWorkingDeviceSecretAsync(string deviceSecret)
    {
        await _store.SetAsync(DeviceSecretKey, deviceSecret).ConfigureAwait(false);
        await _store.RemoveAsync(StagingDeviceSecretKey).ConfigureAwait(false);
    }
}
