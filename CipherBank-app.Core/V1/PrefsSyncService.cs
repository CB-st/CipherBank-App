// <copyright file="PrefsSyncService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;
using CipherBank_app.Persist;

namespace CipherBank_app.V1;

/// <inheritdoc />
public sealed class PrefsSyncService : IPrefsSyncService
{
    private readonly IPrefsStore _store;
    private readonly IProductClient _api;
    private readonly ProductSessionAuthority? _authority;

    public PrefsSyncService(
        IPrefsStore store,
        IProductClient api)
        : this(store, api, null)
    {
    }

    public PrefsSyncService(IPrefsStore store, IProductClient api, ProductSessionAuthority? authority)
    {
        _store = store;
        _authority = authority;
        _api = api;
    }

    public Task PullMergeAsync(CancellationToken ct)
        => PullMergeAsync(_authority?.Capture(), _authority, ct);

    public Task<bool> SaveAndPushAsync(UserPrefs prefs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        return SaveAndPushCoreAsync(prefs, ct);
    }

    internal async Task PullMergeAsync(ProductSessionOperation? operation, ProductSessionAuthority? authority, CancellationToken ct)
    {
        authority ??= _authority;
        UserPrefs local = await _store.LoadAsync(ct).ConfigureAwait(false);
        PrefsWireDto? remote = await _api.GetPrefsAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        if (authority is not null && operation is not null)
        {
            await authority.RunLocalAsync(
                operation,
                async () =>
                {
                    PrefsMerge.Merge(local, remote);
                    await _store.SaveAsync(local, ct).ConfigureAwait(false);
                },
                ct).ConfigureAwait(false);
        }
        else
        {
            PrefsMerge.Merge(local, remote);
            await _store.SaveAsync(local, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Saves locally then pushes prefs after argument validation.
    /// Use: High (SaveAndPushAsync). Scope: this service.
    /// </summary>
    private async Task<bool> SaveAndPushCoreAsync(UserPrefs prefs, CancellationToken ct)
    {
        ProductSessionOperation? operation = _authority?.Capture();
        prefs.NormalizeHomeSections();
        ct.ThrowIfCancellationRequested();
        if (_authority is not null && operation is not null)
        {
            await _authority.RunLocalAsync(operation, () => _store.SaveAsync(prefs, ct), ct).ConfigureAwait(false);
        }
        else
        {
            await _store.SaveAsync(prefs, ct).ConfigureAwait(false);
        }

        bool success = true;
        try
        {
            await _api.PutPrefsAsync(PrefsWireDto.FromUserPrefs(prefs), ct).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            success = false;
        }
        catch (TaskCanceledException)
        {
            success = false;
        }
        catch (JsonException)
        {
            success = false;
        }
        catch (InvalidOperationException)
        {
            success = false;
        }
        catch (ArgumentException)
        {
            success = false;
        }

        ct.ThrowIfCancellationRequested();
        if (_authority is not null && operation is not null)
        {
            _authority.Check(operation, ct);
        }

        return success;
    }
}
