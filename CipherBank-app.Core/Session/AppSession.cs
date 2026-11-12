// <copyright file="AppSession.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.WebSockets;
using System.Text.Json;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Session;

/// <inheritdoc />
public sealed class AppSession : IAppSession, IDisposable
{
    public static readonly int DefaultIdleMs = 60_000;
    private const int MillisecondsPerSecond = 1000;

    private readonly ICustodyService _custody;
    private readonly IProductSessionCoordinator _productSession;
    private readonly ILocalWalletSeeder _seeder;
    private readonly IPrefsStore _prefs;
    private readonly TimeProvider _timeProvider;
    private readonly ProductSessionAuthority _authority;
    private readonly SemaphoreSlim _custodyLane = new(1, 1);
    private DateTimeOffset _lastTouch;
    private int _suppressCustodyEvent;
    private ProductSessionOperation? _accessOperation;
    private ProductSessionOperation? _lastCustodyRevocation;
    private string? _accessToken;
    private Task _pendingDisconnect = Task.CompletedTask;

    public AppSession(
        ICustodyService custody,
        IProductSessionCoordinator productSession,
        ILocalWalletSeeder seeder,
        IPrefsStore prefs,
        TimeProvider timeProvider)
        : this(custody, productSession, seeder, prefs, timeProvider, null)
    {
    }

    public AppSession(
        ICustodyService custody,
        IProductSessionCoordinator productSession,
        ILocalWalletSeeder seeder,
        IPrefsStore prefs,
        TimeProvider timeProvider,
        ProductSessionAuthority? authority)
    {
        _custody = custody;
        _authority = authority ?? (productSession as ProductSessionCoordinator)?.Authority // NOSONAR S3215 — intentional fallback when optional authority is omitted; IProductSessionCoordinator does not expose Authority; upper DI (#98) injects ProductSessionAuthority and concrete ProductSessionCoordinator; changing interface/DI is out of M3b Sonar scope.
            ?? throw new ArgumentException("A shared authority is required for a custom product session coordinator.", nameof(authority));
        _productSession = productSession;
        _seeder = seeder;
        _prefs = prefs;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _lastTouch = _timeProvider.GetUtcNow();
        IdleMs = DefaultIdleMs;
        _custody.Locked += OnCustodyLocked;
    }

    public event EventHandler? Locked;

    public bool IsBooting { get; private set; } = true;

    public bool HasWallet { get; private set; }

    public bool IsUnlocked => _custody.IsUnlocked;

    public int IdleMs { get; set; }

    public string? AccessToken
    {
        get
        {
            string? token = null;
            if (_accessOperation is { } operation)
            {
                try
                {
                    _authority.Commit(operation, () => token = _accessToken);
                }
                catch (OperationCanceledException)
                {
                    // Revocation won the race with this read.
                }
            }

            return token;
        }
    }

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

    public Task<bool> UnlockAsync(string pin)
        => UnlockAsync(pin, _authority.Begin());

    /// <summary>Uses the host's original login identity for custody, result and navigation admission.</summary>
    public async Task<bool> UnlockAsync(string pin, ProductSessionOperation operation)
    {
        if (!await UnlockCustodyAsync(operation, () => _custody.UnlockAsync(pin)).ConfigureAwait(false))
        {
            return false;
        }

        return await CompleteUnlockAsync(operation, applyBootstrap: true).ConfigureAwait(false);
    }

    public Task<bool> CanUnlockWithDeviceOwnerAsync()
        => _custody.CanUnlockWithDeviceOwnerAsync();

    public async Task<bool> UnlockWithDeviceOwnerAsync()
    {
        ProductSessionOperation operation = _authority.Begin();
        if (!await UnlockCustodyAsync(operation, _custody.UnlockWithDeviceSecretAsync).ConfigureAwait(false))
        {
            return false;
        }

        return await CompleteUnlockAsync(operation, applyBootstrap: true).ConfigureAwait(false);
    }

    public void Touch() => _lastTouch = _timeProvider.GetUtcNow();

    public void Lock()
    {
        TearDownProductSession(lockCustody: true);
        Locked?.Invoke(this, EventArgs.Empty);
    }

    public async Task FinishCustodySetupAsync(string mnemonic, string pin)
    {
        ProductSessionOperation operation = _authority.Begin();
        bool unlocked = await UnlockCustodyAsync(operation, async () =>
        {
            await _custody.SealAsync(mnemonic, pin).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
        if (!unlocked)
        {
            throw new OperationCanceledException("Setup was invalidated.");
        }

        _authority.Check(operation);
        await _authority.RunLocalAsync(
            operation,
            () => _seeder.EnsureDerivedAsync(mnemonic),
            operation.Token).ConfigureAwait(false);
        _authority.Check(operation);
        if (!await CompleteUnlockAsync(operation, applyBootstrap: false).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Custody sealed but product session setup failed.");
        }

        _authority.Commit(operation, () => HasWallet = true);
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

    /// <inheritdoc />
    public void Dispose()
    {
        _custody.Locked -= OnCustodyLocked;
        _custodyLane.Dispose();
    }

    private async Task<bool> UnlockCustodyAsync(ProductSessionOperation operation, Func<Task<bool>> unlock)
    {
        await _custodyLane.WaitAsync().ConfigureAwait(false);
        try
        {
            await _authority.RunLocalAsync(operation, () => Task.CompletedTask, operation.Token).ConfigureAwait(false);
            bool result = await unlock().ConfigureAwait(false);
            try
            {
                _authority.Check(operation);
            }
            catch (OperationCanceledException)
            {
                // Replacement custody cannot enter this lane until stale unlock is re-locked.
                _suppressCustodyEvent++;
                try
                {
                    _custody.Lock();
                }
                finally
                {
                    _suppressCustodyEvent--;
                }

                return false;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            _custodyLane.Release();
        }
    }

    /// <summary>
    /// Creates the product session and starts streams after custody is already unlocked.
    /// On session/stream failure, re-locks custody so callers never see unlock-failed with an open vault.
    /// Use: High (every unlock). Scope: AppSession + custody + product session store.
    /// </summary>
    private async Task<bool> CompleteUnlockAsync(ProductSessionOperation operation, bool applyBootstrap)
    {
        await _pendingDisconnect.ConfigureAwait(false);
        try
        {
            _authority.Check(operation);
            ProductSessionStartResult result = _productSession is ProductSessionCoordinator coordinator
                ? await coordinator.StartAsync(operation, applyBootstrap, operation.Token).ConfigureAwait(false)
                : await _productSession.StartAsync(applyBootstrap, operation.Token).ConfigureAwait(false);
            _authority.Commit(operation, () =>
            {
                _accessToken = result.AccessToken;
                _accessOperation = operation;
                IdleMs = result.LockIdleSeconds > 0
                    ? result.LockIdleSeconds * MillisecondsPerSecond
                    : DefaultIdleMs;

                Touch();
            });
            return true;
        }
        catch (InvalidOperationException)
        {
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
        catch (FormatException)
        {
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
        catch (ArgumentException)
        {
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
        catch (SqliteException)
        {
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
        catch (WebSocketException)
        {
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Includes TaskCanceledException from connect/request timeouts.
            return await FailedUnlockAsync(operation).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Tears down product/stream state when custody self-locks (TTL) without going through <see cref="Lock"/>.
    /// Use: High (custody Locked). Scope: AppSession product teardown.
    /// </summary>
    private void OnCustodyLocked(object? sender, EventArgs e)
    {
        if (_suppressCustodyEvent > 0)
        {
            return;
        }

        if (_lastCustodyRevocation is { } revoked)
        {
            try
            {
                _authority.Check(revoked);
                return; // Duplicate event with no intervening attempt; never use AccessToken as this guard.
            }
            catch (OperationCanceledException)
            {
                // A new attempt, including one still inside custody, must be revoked.
            }
        }

        TearDownProductSession();
        Locked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears access token, product session store, and queues stream disconnect.
    /// Use: High (lock paths). Scope: AppSession.
    /// </summary>
    private void TearDownProductSession(bool lockCustody = false)
    {
        _authority.Invalidate(onRevoked: () =>
        {
            _productSession.StopSession();
            _accessToken = null;
            _accessOperation = null;
            if (lockCustody)
            {
                _suppressCustodyEvent++;
                try
                {
                    _custody.Lock();
                }
                finally
                {
                    _suppressCustodyEvent--;
                }
            }

            QueueDisconnect();
            _lastCustodyRevocation = _authority.Capture();
        });
    }

    /// <summary>
    /// Rolls custody and product tokens back when post-unlock session setup fails.
    /// Use: Low (failure path). Scope: AppSession unlock teardown.
    /// </summary>
    private async Task<bool> FailedUnlockAsync(ProductSessionOperation operation)
    {
        bool owned = false;
        try
        {
            _authority.Commit(operation, () =>
            {
                owned = true;
                TearDownProductSession(lockCustody: true);
            });
        }
        catch (OperationCanceledException)
        {
            // A stale failure has no authority over a newer unlocked custody/session.
        }

        if (owned)
        {
            await _pendingDisconnect.ConfigureAwait(false);
            await _authority.Drain.ConfigureAwait(false);
        }

        return false;
    }

    /// <summary>
    /// Starts stream teardown without racing a later <see cref="CompleteUnlockAsync"/> connect.
    /// Use: Medium (lock / unlock failure). Scope: AppSession stream lifecycle.
    /// </summary>
    private void QueueDisconnect()
        => _pendingDisconnect = DisconnectQuietlyAsync();

    private async Task DisconnectQuietlyAsync()
    {
        try
        {
            await _productSession.DisconnectAsync().ConfigureAwait(false);
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
