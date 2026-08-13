// <copyright file="IPrefsStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite-backed prefs.</summary>
public interface IPrefsStore
{
    Task<UserPrefs> LoadAsync();

    Task SaveAsync(UserPrefs prefs);
}
