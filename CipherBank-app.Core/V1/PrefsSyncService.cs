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

    public PrefsSyncService(IPrefsStore store, IProductClient api)
    {
        _store = store;
        _api = api;
    }

    public async Task PullMergeAsync(CancellationToken ct)
    {
        UserPrefs local = await _store.LoadAsync().ConfigureAwait(false);
        PrefsWireDto? remote = await _api.GetPrefsAsync(ct).ConfigureAwait(false);
        PrefsMerge.Merge(local, remote);
        await _store.SaveAsync(local).ConfigureAwait(false);
    }

    public Task<bool> SaveAndPushAsync(UserPrefs prefs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        return SaveAndPushCoreAsync(prefs, ct);
    }

    /// <summary>
    /// Saves locally then pushes prefs after argument validation.
    /// Use: High (SaveAndPushAsync). Scope: this service.
    /// </summary>
    private async Task<bool> SaveAndPushCoreAsync(UserPrefs prefs, CancellationToken ct)
    {
        prefs.NormalizeHomeSections();
        await _store.SaveAsync(prefs).ConfigureAwait(false);
        try
        {
            await _api.PutPrefsAsync(PrefsWireDto.FromUserPrefs(prefs), ct).ConfigureAwait(false);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
