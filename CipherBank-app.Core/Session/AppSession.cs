// <copyright file="AppSession.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.V1;
using CipherBank_app.Wallets;

namespace CipherBank_app.Session;

/// <summary>App-level session: custody unlock + product tokens + idle lock.</summary>
public interface IAppSession
{
    bool IsBooting { get; }

    bool HasWallet { get; }

    bool IsUnlocked { get; }

    int IdleMs { get; set; }

    string? AccessToken { get; }

    event EventHandler? Locked;

    Task BootAsync();

    Task<bool> UnlockAsync(string pin);

    /// <summary>Unlock after successful OS biometrics (device-secret path).</summary>
    Task<bool> UnlockWithDeviceOwnerAsync();

    Task<bool> CanUnlockWithDeviceOwnerAsync();

    void Touch();

    void Lock();

    Task FinishCustodySetupAsync(string mnemonic, string pin);

    /// <summary>Returns true if idle exceeded and lock was applied.</summary>
    bool CheckIdleAndMaybeLock();
}

/// <inheritdoc />
public sealed class AppSession : IAppSession
{
    public const int DefaultIdleMs = 60_000;

    private readonly ICustodyService _custody;
    private readonly IProductApi _api;
    private readonly IStreamService _stream;
    private readonly ILocalWalletSeeder _seeder;
    private readonly IPrefsStore _prefs;
    private readonly IProductSessionStore _productSessions;
    private DateTimeOffset _lastTouch = DateTimeOffset.UtcNow;

    public AppSession(
        ICustodyService custody,
        IProductApi api,
        IStreamService stream,
        ILocalWalletSeeder seeder,
        IPrefsStore prefs,
        IProductSessionStore productSessions)
    {
        _custody = custody;
        _api = api;
        _stream = stream;
        _seeder = seeder;
        _prefs = prefs;
        _productSessions = productSessions;
        IdleMs = DefaultIdleMs;
    }

    public bool IsBooting { get; private set; } = true;

    public bool HasWallet { get; private set; }

    public bool IsUnlocked => _custody.IsUnlocked;

    public int IdleMs { get; set; }

    public string? AccessToken { get; private set; }

    public event EventHandler? Locked;

    public async Task BootAsync()
    {
        IsBooting = true;
        try
        {
            HasWallet = await _custody.HasSealedWalletAsync().ConfigureAwait(false);
            var prefs = await _prefs.LoadAsync().ConfigureAwait(false);
            IdleMs = prefs.LockIdleSeconds > 0 ? prefs.LockIdleSeconds * 1000 : DefaultIdleMs;
        }
        finally
        {
            IsBooting = false;
        }
    }

    public async Task<bool> UnlockAsync(string pin)
    {
        if (!await _custody.UnlockAsync(pin).ConfigureAwait(false))
        {
            return false;
        }

        return await CompleteUnlockAsync().ConfigureAwait(false);
    }

    public Task<bool> CanUnlockWithDeviceOwnerAsync()
        => _custody.CanUnlockWithDeviceOwnerAsync();

    public async Task<bool> UnlockWithDeviceOwnerAsync()
    {
        if (!await _custody.UnlockWithDeviceSecretAsync().ConfigureAwait(false))
        {
            return false;
        }

        return await CompleteUnlockAsync().ConfigureAwait(false);
    }

    public void Touch() => _lastTouch = DateTimeOffset.UtcNow;

    private async Task<bool> CompleteUnlockAsync()
    {
        var session = await _api.CreateSessionAsync().ConfigureAwait(false);
        AccessToken = session.AccessToken;
        await _stream.ConnectAsync().ConfigureAwait(false);
        Touch();
        return true;
    }

    public void Lock()
    {
        _custody.Lock();
        AccessToken = null;
        _productSessions.Clear();
        _ = _stream.DisconnectAsync();
        Locked?.Invoke(this, EventArgs.Empty);
    }

    public async Task FinishCustodySetupAsync(string mnemonic, string pin)
    {
        await _custody.SealAsync(mnemonic, pin).ConfigureAwait(false);
        await _seeder.EnsureDerivedAsync(mnemonic).ConfigureAwait(false);
        var session = await _api.CreateSessionAsync().ConfigureAwait(false);
        AccessToken = session.AccessToken;
        await _stream.ConnectAsync().ConfigureAwait(false);
        HasWallet = true;
        Touch();
    }

    /// <summary>Returns true if idle exceeded and lock was applied.</summary>
    public bool CheckIdleAndMaybeLock()
    {
        if (!IsUnlocked)
        {
            return false;
        }

        if ((DateTimeOffset.UtcNow - _lastTouch).TotalMilliseconds < IdleMs)
        {
            return false;
        }

        Lock();
        return true;
    }
}
