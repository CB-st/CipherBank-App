// <copyright file="RatesCache.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RatesCache : IRatesCache
{
    private readonly ILocalDb _db;

    public RatesCache(ILocalDb db) => _db = db;

    /// <inheritdoc />
    public async Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct)
    {
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = (Microsoft.Data.Sqlite.SqliteTransaction)await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        foreach (RateRow row in rows)
        {
            await using SqliteCommand cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO rates_snapshot (symbol, usd, change24h, updated_at)
                VALUES ($symbol, $usd, $change24h, $updatedAt)
                ON CONFLICT(symbol) DO UPDATE SET
                  usd=$usd, change24h=$change24h, updated_at=$updatedAt
                """;
            cmd.Parameters.AddWithValue("$symbol", row.Symbol.ToUpperInvariant());
            cmd.Parameters.AddWithValue("$usd", row.Usd);
            cmd.Parameters.AddWithValue("$change24h", row.Change24h);
            cmd.Parameters.AddWithValue("$updatedAt", row.UpdatedAtMs);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RateRow>> GetAsync(
        IEnumerable<string>? symbols,
        CancellationToken ct)
    {
        string[] requestedSymbols = symbols?
            .Select(symbol => symbol.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT symbol, usd, change24h, updated_at FROM rates_snapshot ORDER BY symbol";

        var rows = new List<RateRow>();
        HashSet<string>? requestedSet = requestedSymbols.Length == 0
            ? null
            : requestedSymbols.ToHashSet(StringComparer.Ordinal);
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        int ordSymbol = reader.GetOrdinal("symbol");
        int ordUsd = reader.GetOrdinal("usd");
        int ordChange24h = reader.GetOrdinal("change24h");
        int ordUpdatedAt = reader.GetOrdinal("updated_at");
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            string symbol = reader.GetString(ordSymbol);
            if (requestedSet is not null && !requestedSet.Contains(symbol))
            {
                continue;
            }

            rows.Add(new RateRow(
                symbol,
                reader.GetDouble(ordUsd),
                reader.GetDouble(ordChange24h),
                reader.GetInt64(ordUpdatedAt)));
        }

        return rows;
    }
}
