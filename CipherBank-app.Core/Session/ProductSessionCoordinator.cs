// <copyright file="ProductSessionCoordinator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;
using CipherBank_app.Persist;
using CipherBank_app.V1;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Session;

/// <inheritdoc />
public sealed class ProductSessionCoordinator : IProductSessionCoordinator, IDisposable
{
    private readonly IProductClient _client;
    private readonly IStreamService _stream;
    private readonly IStreamHub _streamHub;
    private readonly IPrefsStore _prefs;
    private readonly IPrefsSyncService _prefsSync;
    private readonly IAccountBootstrapService _bootstrap;
    private readonly ProductSessionAuthority _productSessions;
    private readonly SemaphoreSlim _streamLane = new(1, 1);
    private ProductSessionOperation? _streamOwner;

    public ProductSessionCoordinator(
        IProductClient client,
        IStreamService stream,
        IStreamHub streamHub,
        IPrefsStore prefs,
        IPrefsSyncService prefsSync,
        IAccountBootstrapService bootstrap,
        IProductSessionStore productSessions,
        ProductSessionAuthority? authority = null)
    {
        _client = client;
        _stream = stream;
        _streamHub = streamHub;
        _prefs = prefs;
        _prefsSync = prefsSync;
        _bootstrap = bootstrap;
        _productSessions = authority ?? ProductSessionAuthority.For(productSessions);
    }

    internal ProductSessionAuthority Authority => _productSessions;

    /// <inheritdoc />
    public async Task<ProductSessionStartResult> StartAsync(bool applyBootstrap, CancellationToken ct)
    {
        ProductSessionOperation operation = _productSessions.Begin();
        try
        {
            return await StartAsync(operation, applyBootstrap, ct).ConfigureAwait(false);
        }
        catch
        {
            _productSessions.Invalidate(operation);
            await _productSessions.Drain.ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public void StopSession()
    {
        _streamHub.StopStreaming();
        _productSessions.Invalidate();
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        ProductSessionOperation? owner = _streamOwner;
        await _streamLane.WaitAsync().ConfigureAwait(false);
        try
        {
            if (ReferenceEquals(owner, _streamOwner))
            {
                await _stream.DisconnectAsync().ConfigureAwait(false);
                _streamOwner = null;
            }
        }
        finally
        {
            _streamLane.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _streamLane.Dispose();

    internal async Task<ProductSessionStartResult> StartAsync(ProductSessionOperation operation, bool applyBootstrap, CancellationToken ct)
    {
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, operation.Token);
        ct = linked.Token;
        _productSessions.Check(operation, ct);
        await _productSessions.Drain.ConfigureAwait(false);
        UserPrefs localPrefs = await _prefs.LoadAsync(ct).ConfigureAwait(false);
        _productSessions.Check(operation, ct);
        int lockIdleSeconds = localPrefs.LockIdleSeconds;

        // HTTP owns publication; DTO-only providers still require coordinator publication (H1H2 one-publisher test).
        SessionDto session;
        if (_client is HttpProductClient http)
        {
            session = await http.CreateSessionAsync(operation, ct).ConfigureAwait(false);
        }
        else
        {
            session = await _client.CreateSessionAsync(ct).ConfigureAwait(false);
            await _productSessions.PublishAsync(operation, session, ct).ConfigureAwait(false);
        }

        await ConnectOwnedAsync(operation, ct).ConfigureAwait(false);
        try
        {
            try
            {
                if (_prefsSync is PrefsSyncService prefsSync)
                {
                    await prefsSync.PullMergeAsync(operation, _productSessions, ct).ConfigureAwait(false);
                }
                else
                {
                    await _prefsSync.PullMergeAsync(ct).ConfigureAwait(false);
                }

                _productSessions.Check(operation, ct);
                if (applyBootstrap)
                {
                    if (_bootstrap is AccountBootstrapService bootstrap)
                    {
                        await bootstrap.ApplyAsync(operation, _productSessions, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await _bootstrap.ApplyAsync(ct).ConfigureAwait(false);
                    }

                    _productSessions.Check(operation, ct);
                }

                UserPrefs prefs = await _prefs.LoadAsync(ct).ConfigureAwait(false);
                lockIdleSeconds = prefs.LockIdleSeconds;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                // Preference/bootstrap refresh is best-effort after session establishment.
            }
            catch (FormatException)
            {
                // Preference/bootstrap refresh is best-effort after session establishment.
            }
            catch (ArgumentException)
            {
                // Preference/bootstrap refresh is best-effort after session establishment.
            }
            catch (SqliteException)
            {
                // Preference/bootstrap refresh is best-effort after session establishment.
            }
            catch (DbUpdateException)
            {
                // Preference/bootstrap refresh is best-effort after session establishment.
            }
            catch (JsonException)
            {
                // Preference/bootstrap refresh is best-effort after session establishment.
            }
            catch (HttpRequestException)
            {
                // Preference/bootstrap refresh is best-effort after session establishment.
            }

            // Invalidation must escape all best-effort refresh catches.
            _productSessions.Check(operation, ct);
            return new ProductSessionStartResult(session.AccessToken, lockIdleSeconds);
        }
        catch
        {
            await DisconnectOwnedAsync(operation).ConfigureAwait(false);
            throw;
        }
    }

    private async Task ConnectOwnedAsync(ProductSessionOperation operation, CancellationToken ct)
    {
        await _streamLane.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _productSessions.Check(operation, ct);
            if (_streamOwner is not null)
            {
                _streamHub.StopStreaming();
                await _stream.DisconnectAsync().ConfigureAwait(false);
                _productSessions.Check(operation, ct);
            }

            _streamOwner = operation;
            // Subscribe before connect to retain early frames.
            _productSessions.Commit(operation, _streamHub.Start);
            try
            {
                await _stream.ConnectAsync(ct).ConfigureAwait(false);
                _productSessions.Check(operation, ct);
            }
            catch
            {
                _streamHub.StopStreaming();
                await _stream.DisconnectAsync().ConfigureAwait(false);
                _streamOwner = null;
                throw;
            }
        }
        finally
        {
            _streamLane.Release();
        }
    }

    private async Task DisconnectOwnedAsync(ProductSessionOperation operation)
    {
        await _streamLane.WaitAsync().ConfigureAwait(false);
        try
        {
            if (ReferenceEquals(_streamOwner, operation))
            {
                _streamHub.StopStreaming();
                await _stream.DisconnectAsync().ConfigureAwait(false);
                _streamOwner = null;
            }
        }
        finally
        {
            _streamLane.Release();
        }
    }
}
