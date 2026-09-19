// <copyright file="IPrefsStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// SQLite-backed mutable user preferences with an explicit stable JSON wire shape.
/// </summary>
public interface IPrefsStore
{
    /// <summary>
    /// Loads the on-device <c>user_prefs</c> JSON bag, or defaults when the row is missing or invalid.
    /// Use: High (home / settings). Scope: IPrefsStore consumers.
    /// </summary>
    Task<UserPrefs> LoadAsync() => LoadAsync(CancellationToken.None);

    Task<UserPrefs> LoadAsync(CancellationToken ct);

    /// <summary>
    /// Serializes <paramref name="prefs"/> with System.Text.Json and upserts the <c>user_prefs</c> row.
    /// Use: High (settings save). Scope: IPrefsStore consumers.
    /// </summary>
    Task SaveAsync(UserPrefs prefs) => SaveAsync(prefs, CancellationToken.None);

    Task SaveAsync(UserPrefs prefs, CancellationToken ct);
}
