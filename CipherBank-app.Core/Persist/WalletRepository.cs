// <copyright file="WalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using Microsoft.Data.Sqlite;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class WalletRepository : IWalletRepository
{
    private readonly ILocalDb _db;

    public WalletRepository(ILocalDb db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LocalWalletRow>> ListAsync()
    {
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, symbol, label, address, path, account_index, kind, created_at FROM wallets ORDER BY created_at";
        var list = new List<LocalWalletRow>();
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        var ordId = reader.GetOrdinal("id");
        var ordSymbol = reader.GetOrdinal("symbol");
        var ordLabel = reader.GetOrdinal("label");
        var ordAddress = reader.GetOrdinal("address");
        var ordPath = reader.GetOrdinal("path");
        var ordAccountIndex = reader.GetOrdinal("account_index");
        var ordKind = reader.GetOrdinal("kind");
        var ordCreatedAt = reader.GetOrdinal("created_at");
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            list.Add(new LocalWalletRow(
                reader.GetString(ordId),
                reader.GetString(ordSymbol),
                await ReadOptionalStringAsync(reader, ordLabel).ConfigureAwait(false),
                await ReadOptionalStringAsync(reader, ordAddress).ConfigureAwait(false),
                await ReadOptionalStringAsync(reader, ordPath).ConfigureAwait(false),
                reader.GetInt32(ordAccountIndex),
                reader.GetString(ordKind),
                DateTimeOffset.Parse(reader.GetString(ordCreatedAt), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    public async Task UpsertAsync(LocalWalletRow row)
    {
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
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
        cmd.Parameters.AddWithValue("$created", row.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM wallets WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a nullable TEXT column without sync IsDBNull.
    /// Use: High (wallet list). Scope: WalletRepository row hydrate.
    /// </summary>
    private static async Task<string?> ReadOptionalStringAsync(Microsoft.Data.Sqlite.SqliteDataReader reader, int ordinal)
        => await reader.IsDBNullAsync(ordinal).ConfigureAwait(false) ? null : reader.GetString(ordinal);
}
