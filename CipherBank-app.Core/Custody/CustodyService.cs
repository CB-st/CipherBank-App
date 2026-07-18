// <copyright file="CustodyService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>On-device custody seal/unlock (Cora custody.ts parity).</summary>
public interface ICustodyService
{
    Task<bool> HasSealedWalletAsync();

    Task SealAsync(string mnemonic, string pin);

    Task<bool> UnlockAsync(string pin);

    void Lock();

    bool IsUnlocked { get; }

    string? ExportMnemonic();

    DateTimeOffset? SessionExpiresAt { get; }
}

/// <inheritdoc />
public sealed class CustodyService : ICustodyService
{
    private const string BlobKey = "cb_custody_blob";
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

    public bool IsUnlocked => _mnemonic is not null && _expires is DateTimeOffset e && e > DateTimeOffset.UtcNow;

    public DateTimeOffset? SessionExpiresAt => _expires;

    public async Task<bool> HasSealedWalletAsync()
        => !string.IsNullOrEmpty(await _store.GetAsync(BlobKey).ConfigureAwait(false));

    public async Task SealAsync(string mnemonic, string pin)
    {
        string normalized = MnemonicHelper.Normalize(mnemonic);
        if (!MnemonicHelper.Validate(normalized))
        {
            throw new ArgumentException("Invalid mnemonic.", nameof(mnemonic));
        }

        await _pin.SetPinAsync(pin).ConfigureAwait(false);
        string sealedBlob = CryptoBox.Seal(normalized, pin);
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

        try
        {
            _mnemonic = CryptoBox.Open(blob, pin);
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
}
