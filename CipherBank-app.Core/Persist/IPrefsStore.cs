// <copyright file="IPrefsStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite-backed prefs.</summary>
public interface IPrefsStore
{
    Task<UserPrefs> LoadAsync();

    Task SaveAsync(UserPrefs prefs);
}
