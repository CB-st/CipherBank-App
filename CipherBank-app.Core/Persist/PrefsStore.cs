// <copyright file="PrefsStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class PrefsStore : IPrefsStore
{
    private const string Key = "user_prefs";
    private readonly ILocalDb _db;

    public PrefsStore(ILocalDb db)
    {
        _db = db;
    }

    public async Task<UserPrefs> LoadAsync()
    {
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM prefs WHERE key=$k";
        cmd.Parameters.AddWithValue("$k", Key);
        var val = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
        UserPrefs prefs = val is string json && !string.IsNullOrWhiteSpace(json)
            ? JsonSerializer.Deserialize<UserPrefs>(json) ?? new UserPrefs()
            : new UserPrefs();

        prefs.NormalizeHomeSections();
        return prefs;
    }

    public async Task SaveAsync(UserPrefs prefs)
    {
        prefs.NormalizeHomeSections();
        var json = JsonSerializer.Serialize(prefs);
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO prefs (key, value) VALUES ($k, $v)
            ON CONFLICT(key) DO UPDATE SET value=$v
            """;
        cmd.Parameters.AddWithValue("$k", Key);
        cmd.Parameters.AddWithValue("$v", json);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
