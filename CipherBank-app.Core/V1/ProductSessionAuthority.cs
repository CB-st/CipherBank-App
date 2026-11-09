// <copyright file="ProductSessionAuthority.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Runtime.CompilerServices;

namespace CipherBank_app.V1;

/// <summary>
/// Coordinates a legacy backing store for the uninterrupted process lifetime.
/// Raw backing calls after composition bypass this contract. Crash durability is not provided.
/// </summary>
public sealed class ProductSessionAuthority : IDisposable
{
    private static readonly ConditionalWeakTable<IProductSessionStore, ProductSessionAuthority> Authorities = new();
    private readonly IProductSessionStore _backing;
    private readonly object _state = new();
    private readonly SemaphoreSlim _storage = new(1, 1);
    private CancellationTokenSource _lifetime = new();
    private long _epoch;
    private long _revision;
    private bool _loaded;
    private ProductSessionSnapshot? _snapshot;
    private Task _drain = Task.CompletedTask;
    private bool _revoking;
    private ProductSessionRefreshFlight? _refresh;

    private ProductSessionAuthority(IProductSessionStore backing) => _backing = backing;

    /// <summary>The observable physical cleanup completion, including cleanup failure.</summary>
    public Task Drain
    {
        get
        {
            lock (_state)
            {
                return _drain;
            }
        }
    }

    /// <summary>Shares coordination even for legacy callers using direct constructors.</summary>
    public static ProductSessionAuthority For(IProductSessionStore backing)
    {
        ArgumentNullException.ThrowIfNull(backing);
        return Authorities.GetValue(backing, static store => new ProductSessionAuthority(store));
    }

    /// <summary>Supersedes prior attempts before custody or remote work begins.</summary>
    public ProductSessionOperation Begin()
    {
        CancellationTokenSource previous;
        ProductSessionOperation operation;
        lock (_state)
        {
            previous = _lifetime;
            _lifetime = new CancellationTokenSource();
            _epoch++;
            operation = CaptureCore();
            _snapshot = null;
            _loaded = true;
        }

        previous.Cancel();
        previous.Dispose();
        return operation;
    }

    /// <summary>Captures the current identity once at an operation boundary.</summary>
    public ProductSessionOperation Capture()
    {
        lock (_state)
        {
            return CaptureCore();
        }
    }

    /// <summary>Checks original ownership, outside best-effort exception handling.</summary>
    public void Check(ProductSessionOperation operation) => Check(operation, CancellationToken.None);

    public void Check(ProductSessionOperation operation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_state)
        {
            CheckCore(operation);
        }
    }

    /// <summary>Atomically admits a synchronous publication against invalidation.</summary>
    public void Commit(ProductSessionOperation operation, Action publish)
    {
        ArgumentNullException.ThrowIfNull(publish);
        lock (_state)
        {
            CheckCore(operation);
            publish();
        }
    }

    /// <summary>Revokes immediately; queues physical removal behind admitted local writes.</summary>
    public void Invalidate() => Invalidate(null, null);

    public void Invalidate(ProductSessionOperation? expected) => Invalidate(expected, null);

    public void Invalidate(Action? onRevoked) => Invalidate(null, onRevoked);

    public void Invalidate(ProductSessionOperation? expected, Action? onRevoked)
    {
        CancellationTokenSource? previous = null;
        try
        {
            lock (_state)
            {
                if (expected is not null && (expected.Epoch != _epoch || !ReferenceEquals(expected.Owner, this)))
                {
                    return;
                }

                // Re-entrant teardown is part of this revoke, not a new epoch or physical clear.
                if (_revoking)
                {
                    onRevoked?.Invoke();
                    return;
                }

                previous = _lifetime;
                _lifetime = new CancellationTokenSource();
                _epoch++;
                _revision++;
                _loaded = true;
                _snapshot = null;
                // Install the observed drain before callbacks can re-enter. Pending cleanup already
                // covers admitted writes; replacements cannot write until that same drain completes.
                TaskCompletionSource? cleanup = null;
                if (_drain.IsCompleted)
                {
                    cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    _drain = cleanup.Task;
                }

                _revoking = true;
                try
                {
                    onRevoked?.Invoke();
                }
                finally
                {
                    _revoking = false;
                    if (cleanup is not null)
                    {
                        _ = CompleteClearAsync(cleanup);
                    }
                }
            }
        }
        finally
        {
            if (previous is not null)
            {
                try
                {
                    previous.Cancel();
                }
                finally
                {
                    previous.Dispose();
                }
            }
        }
    }

    /// <summary>Reads only a complete tuple, validating an initial backing read after its await.</summary>
    public Task<ProductSessionSnapshot?> ReadAsync(ProductSessionOperation operation)
        => ReadAsync(operation, CancellationToken.None);

    public async Task<ProductSessionSnapshot?> ReadAsync(ProductSessionOperation operation, CancellationToken ct)
    {
        try
        {
            return await ReadCoreAsync(operation, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await Drain.ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Serializes physical publication and atomically exposes it only to its original owner.</summary>
    public Task<ProductSessionSnapshot> PublishAsync(
        ProductSessionOperation operation, SessionDto session, CancellationToken ct)
        => PublishAsync(operation, session, ct, null);

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA1068:CancellationToken parameters must come last",
        Justification = "Preserve the existing positional API while replacing optional expectedRevision with an overload.")]
    public async Task<ProductSessionSnapshot> PublishAsync(
        ProductSessionOperation operation, SessionDto session, CancellationToken ct, long? expectedRevision)
    {
        await Drain.ConfigureAwait(false);
        await _storage.WaitAsync(ct).ConfigureAwait(false);
        bool written = false;
        try
        {
            Check(operation, ct);
            lock (_state)
            {
                CheckCore(operation);
                if (_drain.IsFaulted)
                {
                    throw new InvalidOperationException($"Product {nameof(session)} cleanup must recover before publication.", _drain.Exception);
                }

                if (expectedRevision.HasValue && _revision != expectedRevision.Value)
                {
                    throw new OperationCanceledException("Credential revision changed.");
                }

                // Hide partial secure-key writes from every authoritative reader.
                _loaded = true;
                _snapshot = null;
            }

            written = true;
            await _backing.SaveAsync(session).ConfigureAwait(false);
            lock (_state)
            {
                CheckCore(operation);
                ct.ThrowIfCancellationRequested();
                _revision++;
                _snapshot = new ProductSessionSnapshot(
                    operation,
                    _revision,
                    session.AccessToken,
                    session.RefreshToken,
                    DateTimeOffset.FromUnixTimeMilliseconds(session.ExpiresAt));
                return _snapshot;
            }
        }
        catch
        {
            if (written)
            {
                // The lane still belongs to this write. Clear partial keys before any replacement write.
                lock (_state)
                {
                    _snapshot = null;
                    _loaded = true;
                }

                try
                {
                    _backing.Clear();
                }
                catch (Exception cleanupError)
                {
                    lock (_state)
                    {
                        // Retain any queued revoke cleanup as well as this write failure.
                        _drain = Task.WhenAll(_drain, Task.FromException(cleanupError));
                    }

                    throw;
                }
            }

            throw;
        }
        finally
        {
            _storage.Release();
        }
    }

    /// <summary>Drains admitted local effects before replacement; never wrap network work in this lane.</summary>
    public Task RunLocalAsync(ProductSessionOperation operation, Func<Task> action, CancellationToken ct)
    {
        if (action is null)
        {
            // Preserve the original async API's faulted-task contract before entering the storage lane.
            return Task.FromException(new ArgumentNullException(nameof(action)));
        }

        return RunLocalCoreAsync(operation, action, ct);
    }

    /// <summary>Coalesces rotating refresh for an exact epoch/revision; cancellation belongs to each waiter.</summary>
    public async Task<ProductSessionSnapshot> RefreshAsync(
        ProductSessionSnapshot original, Func<CancellationToken, Task<SessionDto>> refresh, CancellationToken ct)
    {
        ProductSessionRefreshFlight flight;
        bool start = false;
        lock (_state)
        {
            CheckCore(original.Operation);
            if (_snapshot is { } current && current.Revision != original.Revision)
            {
                return current;
            }

            if (_refresh is null || NeedsNewRefresh(_refresh, original))
            {
                _refresh = new ProductSessionRefreshFlight(original);
                start = true;
            }

            flight = _refresh;
            flight.Waiters++;
        }

        ProductSessionRefreshWaiter waiter = new();
        using CancellationTokenRegistration cancellation = ct.Register(() => ReleaseWaiter(flight, waiter));
        if (start)
        {
            // Start remote work only after releasing the state monitor.
            _ = RefreshCoreAsync(flight, refresh);
        }

        try
        {
            return await flight.Completion.WaitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            ReleaseWaiter(flight, waiter);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _lifetime.Dispose();
        _storage.Dispose();
        _refresh?.Dispose();
    }

    private async Task RunLocalCoreAsync(ProductSessionOperation operation, Func<Task> action, CancellationToken ct)
    {
        await Drain.ConfigureAwait(false);
        await _storage.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Check(operation, ct);
            lock (_state)
            {
                CheckCore(operation);
                if (_drain.IsFaulted)
                {
                    throw new InvalidOperationException("Product session cleanup must recover before local effects.", _drain.Exception);
                }
            }

            await action().ConfigureAwait(false);
            Check(operation, ct);
        }
        finally
        {
            _storage.Release();
        }
    }

    private async Task<ProductSessionSnapshot?> ReadCoreAsync(ProductSessionOperation operation, CancellationToken ct = default)
    {
        Check(operation, ct);
        lock (_state)
        {
            CheckCore(operation);
            if (_loaded)
            {
                return _snapshot;
            }
        }

        await _storage.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Check(operation, ct);
            lock (_state)
            {
                if (_loaded)
                {
                    return _snapshot;
                }
            }

            var stored = await _backing.GetAsync().ConfigureAwait(false);
            lock (_state)
            {
                CheckCore(operation);
                ct.ThrowIfCancellationRequested();
                _snapshot = stored is { } value
                    ? new ProductSessionSnapshot(operation, _revision, value.Access, value.Refresh, value.Expires) : null;
                _loaded = true;
                return _snapshot;
            }
        }
        finally
        {
            _storage.Release();
        }
    }

    // Keep the B1/H1H2 refresh admission decision under the state monitor.
    private bool NeedsNewRefresh(ProductSessionRefreshFlight flight, ProductSessionSnapshot original)
        => flight.Original.Operation.Epoch != _epoch
            || flight.Original.Revision != original.Revision
            || flight.Completion.IsFaulted
            || flight.Cancellation.IsCancellationRequested;

    private void ReleaseWaiter(ProductSessionRefreshFlight flight, ProductSessionRefreshWaiter waiter)
    {
        lock (_state)
        {
            if (!waiter.TryRelease())
            {
                return;
            }

            flight.Waiters--;
            if (flight.Waiters == 0 && !flight.Completion.IsCompleted)
            {
                flight.Cancellation.Cancel();
            }
        }
    }

    private async Task RefreshCoreAsync(
        ProductSessionRefreshFlight flight, Func<CancellationToken, Task<SessionDto>> refresh)
    {
        try
        {
            SessionDto session = await refresh(flight.Cancellation.Token).ConfigureAwait(false);
            ProductSessionSnapshot published = await PublishAsync(
                flight.Original.Operation, session, flight.Cancellation.Token, flight.Original.Revision).ConfigureAwait(false);
            flight.CompletionSource.TrySetResult(published);
        }
        catch (Exception error) when (!flight.Completion.IsCompleted)
        {
            // This worker alone completes the flight; every failure must release its waiters (H1H2).
            flight.CompletionSource.TrySetException(error);
            // Observe the shared failure even if every waiter cancelled independently.
            _ = flight.Completion.Exception;
        }
    }

    private async Task CompleteClearAsync(TaskCompletionSource completion)
    {
        try
        {
            await ClearAsync().ConfigureAwait(false);
            completion.SetResult();
        }
        catch (Exception error) when (!completion.Task.IsCompleted)
        {
            // Invalidate installs this pending drain before starting its sole cleanup worker (B1).
            completion.SetException(error);
        }
    }

    private async Task ClearAsync()
    {
        await _storage.WaitAsync().ConfigureAwait(false);
        try
        {
            _backing.Clear();
        }
        finally
        {
            _storage.Release();
        }
    }

    private ProductSessionOperation CaptureCore() => new(this, _epoch, _lifetime.Token);

    private void CheckCore(ProductSessionOperation operation)
    {
        if (operation.Epoch != _epoch || !ReferenceEquals(operation.Owner, this))
        {
            throw new OperationCanceledException("Product session was invalidated.", operation.Token);
        }
    }
}
