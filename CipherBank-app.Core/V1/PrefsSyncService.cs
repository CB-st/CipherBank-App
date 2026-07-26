// <copyright file="PrefsSyncService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;

namespace CipherBank_app.V1;

/// <summary>Pull/push user prefs with local SQLite as offline source of truth.</summary>
public interface IPrefsSyncService
{
    /// <summary>GET remote prefs and merge into local store.</summary>
    Task PullMergeAsync(CancellationToken ct);

    /// <summary>Save local prefs then PUT remote. Local write always kept even if PUT fails.</summary>
    Task<bool> SaveAndPushAsync(UserPrefs prefs, CancellationToken ct);

    /// <summary>Pull-merge for callers with no ambient token. Use: Medium (startup sync). Scope: IPrefsSyncService consumers.</summary>
    Task PullMergeAsync() => PullMergeAsync(CancellationToken.None);

    /// <summary>Save-and-push for callers with no ambient token. Use: Medium (Profile toggles). Scope: IPrefsSyncService consumers.</summary>
    Task<bool> SaveAndPushAsync(UserPrefs prefs) => SaveAndPushAsync(prefs, CancellationToken.None);
}

/// <inheritdoc />
public sealed class PrefsSyncService : IPrefsSyncService
{
    private readonly IPrefsStore _store;
    private readonly IProductApi _api;

    public PrefsSyncService(IPrefsStore store, IProductApi api)
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

    public async Task<bool> SaveAndPushAsync(UserPrefs prefs, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        prefs.NormalizeHomeSections();
        await _store.SaveAsync(prefs).ConfigureAwait(false);
        try
        {
            await _api.PutPrefsAsync(PrefsWireDto.FromUserPrefs(prefs), ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
