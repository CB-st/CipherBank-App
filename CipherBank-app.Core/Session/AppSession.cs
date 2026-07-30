// <copyright file="AppSession.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using Microsoft.Data.Sqlite;

namespace CipherBank_app.Session;

/// <inheritdoc />
public sealed class AppSession : IAppSession
{
    public static readonly int DefaultIdleMs = 60_000;
    private const int MillisecondsPerSecond = 1000;

    private readonly ICustodyService _custody;
    private readonly IProductApi _api;
    private readonly IStreamService _stream;
    private readonly IStreamHub _streamHub;
    private readonly ILocalWalletSeeder _seeder;
    private readonly IPrefsStore _prefs;
    private readonly IPrefsSyncService _prefsSync;
    private readonly IAccountBootstrapService _bootstrap;
    private readonly IProductSessionStore _productSessions;
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset _lastTouch;
    private Task _pendingDisconnect = Task.CompletedTask;

    public AppSession(AppSessionDeps deps)
    {
        _custody = deps.Custody;
        _api = deps.Api;
        _stream = deps.Stream;
        _streamHub = deps.StreamHub;
        _seeder = deps.Seeder;
        _prefs = deps.Prefs;
        _prefsSync = deps.PrefsSync;
        _bootstrap = deps.Bootstrap;
        _productSessions = deps.ProductSessions;
        _timeProvider = deps.Time ?? TimeProvider.System;
        _lastTouch = _timeProvider.GetUtcNow();
        IdleMs = DefaultIdleMs;
    }

    public event EventHandler? Locked;

    public bool IsBooting { get; private set; } = true;

    public bool HasWallet { get; private set; }

    public bool IsUnlocked => _custody.IsUnlocked;

    public int IdleMs { get; set; }

    public string? AccessToken { get; private set; }

    public async Task BootAsync()
    {
        IsBooting = true;
        try
        {
            HasWallet = await _custody.HasSealedWalletAsync().ConfigureAwait(false);
            UserPrefs prefs = await _prefs.LoadAsync().ConfigureAwait(false);
            IdleMs = prefs.LockIdleSeconds > 0 ? prefs.LockIdleSeconds * MillisecondsPerSecond : DefaultIdleMs;
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

        return await CompleteUnlockAsync(applyBootstrap: true).ConfigureAwait(false);
    }

    public Task<bool> CanUnlockWithDeviceOwnerAsync()
        => _custody.CanUnlockWithDeviceOwnerAsync();

    public async Task<bool> UnlockWithDeviceOwnerAsync()
    {
        if (!await _custody.UnlockWithDeviceSecretAsync().ConfigureAwait(false))
        {
            return false;
        }

        return await CompleteUnlockAsync(applyBootstrap: true).ConfigureAwait(false);
    }

    public void Touch() => _lastTouch = _timeProvider.GetUtcNow();

    public void Lock()
    {
        _streamHub.StopStreaming();
        _custody.Lock();
        AccessToken = null;
        _productSessions.Clear();
        QueueDisconnect();
        Locked?.Invoke(this, EventArgs.Empty);
    }

    public async Task FinishCustodySetupAsync(string mnemonic, string pin)
    {
        await _custody.SealAsync(mnemonic, pin).ConfigureAwait(false);
        await _seeder.EnsureDerivedAsync(mnemonic).ConfigureAwait(false);
        if (!await CompleteUnlockAsync(applyBootstrap: false).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Custody sealed but product session setup failed.");
        }

        HasWallet = true;
    }

    /// <summary>Returns true if idle exceeded and lock was applied.</summary>
    public bool CheckIdleAndMaybeLock()
    {
        if (!IsUnlocked)
        {
            return false;
        }

        if ((_timeProvider.GetUtcNow() - _lastTouch).TotalMilliseconds < IdleMs)
        {
            return false;
        }

        Lock();
        return true;
    }

    /// <summary>
    /// Creates the product session and starts streams after custody is already unlocked.
    /// On session/stream failure, re-locks custody so callers never see unlock-failed with an open vault.
    /// Use: High (every unlock). Scope: AppSession + custody + product session store.
    /// </summary>
    private async Task<bool> CompleteUnlockAsync(bool applyBootstrap)
    {
        await _pendingDisconnect.ConfigureAwait(false);
        try
        {
            SessionDto session = await _api.CreateSessionAsync(CancellationToken.None).ConfigureAwait(false);
            AccessToken = session.AccessToken;
            await _productSessions.SaveAsync(session).ConfigureAwait(false);
            await _stream.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
            _streamHub.Start();

            try
            {
                await _prefsSync.PullMergeAsync(CancellationToken.None).ConfigureAwait(false);
                if (applyBootstrap)
                {
                    await _bootstrap.ApplyAsync(CancellationToken.None).ConfigureAwait(false);
                }

                UserPrefs prefs = await _prefs.LoadAsync().ConfigureAwait(false);
                IdleMs = prefs.LockIdleSeconds > 0 ? prefs.LockIdleSeconds * MillisecondsPerSecond : DefaultIdleMs;
            }
            catch (InvalidOperationException)
            {
                // Prefs/bootstrap are best-effort after a successful product session.
            }
            catch (FormatException)
            {
                // Prefs/bootstrap are best-effort after a successful product session.
            }
            catch (ArgumentException)
            {
                // Prefs/bootstrap are best-effort after a successful product session.
            }
            catch (SqliteException)
            {
                // Prefs/bootstrap are best-effort after a successful product session.
            }
            catch (JsonException)
            {
                // Prefs/bootstrap are best-effort after a successful product session.
            }

            Touch();
            return true;
        }
        catch (InvalidOperationException)
        {
            RollbackFailedUnlock();
            return false;
        }
        catch (FormatException)
        {
            RollbackFailedUnlock();
            return false;
        }
        catch (ArgumentException)
        {
            RollbackFailedUnlock();
            return false;
        }
        catch (SqliteException)
        {
            RollbackFailedUnlock();
            return false;
        }
        catch (JsonException)
        {
            RollbackFailedUnlock();
            return false;
        }
        catch (HttpRequestException)
        {
            RollbackFailedUnlock();
            return false;
        }
        catch (WebSocketException)
        {
            RollbackFailedUnlock();
            return false;
        }
        catch (OperationCanceledException)
        {
            // Includes TaskCanceledException from connect/request timeouts.
            RollbackFailedUnlock();
            return false;
        }
    }

    /// <summary>
    /// Rolls custody and product tokens back when post-unlock session setup fails.
    /// Use: Low (failure path). Scope: AppSession unlock teardown.
    /// </summary>
    private void RollbackFailedUnlock()
    {
        _streamHub.StopStreaming();
        _custody.Lock();
        AccessToken = null;
        _productSessions.Clear();
        QueueDisconnect();
    }

    /// <summary>
    /// Starts stream teardown without racing a later <see cref="CompleteUnlockAsync"/> connect.
    /// Use: Medium (lock / unlock failure). Scope: AppSession stream lifecycle.
    /// </summary>
    private void QueueDisconnect()
    {
        _pendingDisconnect = DisconnectQuietlyAsync();
    }

    private async Task DisconnectQuietlyAsync()
    {
        try
        {
            await _stream.DisconnectAsync().ConfigureAwait(false);
        }
        catch (WebSocketException)
        {
            // ignored — socket may already be closing
        }
        catch (ObjectDisposedException)
        {
            // ignored
        }
        catch (InvalidOperationException)
        {
            // ignored
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }
}
