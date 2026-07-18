// <copyright file="PrefsStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;

namespace CipherBank_app.Persist;

/// <summary>User preference model (Cora prefs).</summary>
public sealed class UserPrefs
{
    public List<string> HomeOrder { get; set; } = new() { "cora", "balance", "quickActions", "performance", "assets" };

    public Dictionary<string, bool> HomeVisible { get; set; } = new()
    {
        ["cora"] = true,
        ["balance"] = true,
        ["quickActions"] = true,
        ["performance"] = true,
        ["assets"] = true,
    };

    public bool ValuesHiddenOnLaunch { get; set; }

    public bool CoraEnabled { get; set; } = true;

    public string DefaultSendSpeed { get; set; } = "instant";

    public string Appearance { get; set; } = "dark";

    public string BaseCurrency { get; set; } = "USD";

    public int LockIdleSeconds { get; set; } = 120;
}

/// <summary>SQLite-backed prefs.</summary>
public interface IPrefsStore
{
    Task<UserPrefs> LoadAsync();

    Task SaveAsync(UserPrefs prefs);
}

/// <inheritdoc />
public sealed class PrefsStore : IPrefsStore
{
    private const string Key = "user_prefs";
    private readonly ILocalDb _db;

    public PrefsStore(ILocalDb db) => _db = db;

    public async Task<UserPrefs> LoadAsync()
    {
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM prefs WHERE key=$k";
        cmd.Parameters.AddWithValue("$k", Key);
        object? val = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
        if (val is string json && !string.IsNullOrWhiteSpace(json))
        {
            return JsonSerializer.Deserialize<UserPrefs>(json) ?? new UserPrefs();
        }

        return new UserPrefs();
    }

    public async Task SaveAsync(UserPrefs prefs)
    {
        string json = JsonSerializer.Serialize(prefs);
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO prefs (key, value) VALUES ($k, $v)
            ON CONFLICT(key) DO UPDATE SET value=$v
            """;
        cmd.Parameters.AddWithValue("$k", Key);
        cmd.Parameters.AddWithValue("$v", json);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
