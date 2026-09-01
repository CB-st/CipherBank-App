// <copyright file="ProductSessionCoordinator.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;
using CipherBank_app.Persist;
using CipherBank_app.V1;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Session;

/// <inheritdoc />
public sealed class ProductSessionCoordinator : IProductSessionCoordinator
{
    private readonly IProductClient _client;
    private readonly IStreamService _stream;
    private readonly IStreamHub _streamHub;
    private readonly IPrefsStore _prefs;
    private readonly IPrefsSyncService _prefsSync;
    private readonly IAccountBootstrapService _bootstrap;
    private readonly IProductSessionStore _productSessions;

    public ProductSessionCoordinator(
        IProductClient client,
        IStreamService stream,
        IStreamHub streamHub,
        IPrefsStore prefs,
        IPrefsSyncService prefsSync,
        IAccountBootstrapService bootstrap,
        IProductSessionStore productSessions)
    {
        _client = client;
        _stream = stream;
        _streamHub = streamHub;
        _prefs = prefs;
        _prefsSync = prefsSync;
        _bootstrap = bootstrap;
        _productSessions = productSessions;
    }

    /// <inheritdoc />
    public async Task<ProductSessionStartResult> StartAsync(bool applyBootstrap, CancellationToken ct)
    {
        // Preserve any already-loaded local idle timeout if refresh fails below.
        UserPrefs localPrefs = await _prefs.LoadAsync().ConfigureAwait(false);
        int lockIdleSeconds = localPrefs.LockIdleSeconds;

        SessionDto session = await _client.CreateSessionAsync(ct).ConfigureAwait(false);
        await _productSessions.SaveAsync(session).ConfigureAwait(false);

        // Subscribe before the socket is live so early frames are not dropped.
        _streamHub.Start();
        try
        {
            await _stream.ConnectAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            _streamHub.StopStreaming();
            throw;
        }

        try
        {
            await _prefsSync.PullMergeAsync(ct).ConfigureAwait(false);
            if (applyBootstrap)
            {
                await _bootstrap.ApplyAsync(ct).ConfigureAwait(false);
            }

            UserPrefs prefs = await _prefs.LoadAsync().ConfigureAwait(false);
            lockIdleSeconds = prefs.LockIdleSeconds;
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
        catch (OperationCanceledException)
        {
            // Preference/bootstrap refresh is best-effort after session establishment.
        }

        return new ProductSessionStartResult(session.AccessToken, lockIdleSeconds);
    }

    /// <inheritdoc />
    public void StopSession()
    {
        _streamHub.StopStreaming();
        _productSessions.Clear();
    }

    /// <inheritdoc />
    public Task DisconnectAsync() => _stream.DisconnectAsync();
}
