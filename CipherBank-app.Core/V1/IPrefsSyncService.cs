// <copyright file="IPrefsSyncService.cs" company="CipherBank">
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
