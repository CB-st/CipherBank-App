// <copyright file="CustodyService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.Custody;

/// <summary>On-device custody seal/unlock (Cora custody.ts parity).</summary>
public interface ICustodyService
{
    Task<bool> HasSealedWalletAsync();

    /// <summary>True when a device secret exists so OS-auth unlock is possible.</summary>
    Task<bool> CanUnlockWithDeviceOwnerAsync();

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
        => !string.IsNullOrEmpty(await _store.GetAsync(DeviceSecretKey).ConfigureAwait(false));

    public async Task SealAsync(string mnemonic, string pin)
    {
        string normalized = MnemonicHelper.Normalize(mnemonic);
        if (!MnemonicHelper.Validate(normalized))
        {
            throw new ArgumentException("Invalid mnemonic.", nameof(mnemonic));
        }

        await _pin.SetPinAsync(pin).ConfigureAwait(false);
        string deviceSecret = CreateDeviceSecret();
        string sealedBlob = CryptoBox.Seal(normalized, deviceSecret);
        await _store.SetAsync(DeviceSecretKey, deviceSecret).ConfigureAwait(false);
        await _store.SetAsync(BlobKey, sealedBlob).ConfigureAwait(false);
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

        string? deviceSecret = await _store.GetAsync(DeviceSecretKey).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(deviceSecret))
            {
                // Expo shape: PIN is a logical gate; AES key is the device secret.
                _mnemonic = CryptoBox.Open(blob, deviceSecret);
            }
            else
            {
                // Legacy PIN-derived blob — migrate to device-secret seal once unlocked.
                _mnemonic = CryptoBox.Open(blob, pin);
                await MigrateToDeviceSecretAsync(_mnemonic).ConfigureAwait(false);
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
        string? deviceSecret = await _store.GetAsync(DeviceSecretKey).ConfigureAwait(false);
        string? blob = await _store.GetAsync(BlobKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(deviceSecret) || string.IsNullOrEmpty(blob))
        {
            return false;
        }

        try
        {
            _mnemonic = CryptoBox.Open(blob, deviceSecret);
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

    private async Task MigrateToDeviceSecretAsync(string mnemonic)
    {
        string deviceSecret = CreateDeviceSecret();
        string sealedBlob = CryptoBox.Seal(mnemonic, deviceSecret);
        await _store.SetAsync(DeviceSecretKey, deviceSecret).ConfigureAwait(false);
        await _store.SetAsync(BlobKey, sealedBlob).ConfigureAwait(false);
    }

    private static string CreateDeviceSecret()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
