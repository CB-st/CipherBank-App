// <copyright file="WalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;

namespace CipherBank_app.Persist;

/// <summary>Local wallet index row.</summary>
public sealed record LocalWalletRow(
    string Id,
    string Symbol,
    string? Label,
    string? Address,
    string? Path,
    int AccountIndex,
    string Kind,
    DateTimeOffset CreatedAt);

/// <summary>SQLite wallets repo.</summary>
public interface IWalletRepository
{
    Task<IReadOnlyList<LocalWalletRow>> ListAsync();

    Task UpsertAsync(LocalWalletRow row);

    Task DeleteAsync(string id);
}

/// <inheritdoc />
public sealed class WalletRepository : IWalletRepository
{
    private readonly ILocalDb _db;

    public WalletRepository(ILocalDb db) => _db = db;

    public async Task<IReadOnlyList<LocalWalletRow>> ListAsync()
    {
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, symbol, label, address, path, account_index, kind, created_at FROM wallets ORDER BY created_at";
        var list = new List<LocalWalletRow>();
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            list.Add(new LocalWalletRow(
                reader.GetString(0),
                reader.GetString(1),
                await ReadOptionalStringAsync(reader, 2).ConfigureAwait(false),
                await ReadOptionalStringAsync(reader, 3).ConfigureAwait(false),
                await ReadOptionalStringAsync(reader, 4).ConfigureAwait(false),
                reader.GetInt32(5),
                reader.GetString(6),
                DateTimeOffset.Parse(reader.GetString(7), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    /// <summary>
    /// Reads a nullable TEXT column without sync IsDBNull.
    /// Use: High (wallet list). Scope: WalletRepository row hydrate.
    /// </summary>
    private static async Task<string?> ReadOptionalStringAsync(Microsoft.Data.Sqlite.SqliteDataReader reader, int ordinal)
        => await reader.IsDBNullAsync(ordinal).ConfigureAwait(false) ? null : reader.GetString(ordinal);

    public async Task UpsertAsync(LocalWalletRow row)
    {
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO wallets (id, symbol, label, address, path, account_index, kind, created_at)
            VALUES ($id, $symbol, $label, $address, $path, $idx, $kind, $created)
            ON CONFLICT(id) DO UPDATE SET
              symbol=$symbol, label=$label, address=$address, path=$path, account_index=$idx, kind=$kind
            """;
        cmd.Parameters.AddWithValue("$id", row.Id);
        cmd.Parameters.AddWithValue("$symbol", row.Symbol);
        cmd.Parameters.AddWithValue("$label", (object?)row.Label ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$address", (object?)row.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$path", (object?)row.Path ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$idx", row.AccountIndex);
        cmd.Parameters.AddWithValue("$kind", row.Kind);
        cmd.Parameters.AddWithValue("$created", row.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM wallets WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
