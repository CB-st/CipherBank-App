// <copyright file="RatesCache.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RatesCache : IRatesCache
{
    private readonly ILocalDb _db;

    public RatesCache(ILocalDb db) => _db = db;

    /// <inheritdoc />
    public async Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct = default)
    {
        await using var conn = _db.Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = conn.BeginTransaction();
        foreach (RateRow row in rows)
        {
            await using var cmd = conn.CreateCommand();
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
        IEnumerable<string>? symbols = null,
        CancellationToken ct = default)
    {
        string[] requestedSymbols = symbols?
            .Select(symbol => symbol.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        await using var conn = _db.Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT symbol, usd, change24h, updated_at FROM rates_snapshot ORDER BY symbol";

        var rows = new List<RateRow>();
        HashSet<string>? requestedSet = requestedSymbols.Length == 0
            ? null
            : requestedSymbols.ToHashSet(StringComparer.Ordinal);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            string symbol = reader.GetString(0);
            if (requestedSet is not null && !requestedSet.Contains(symbol))
            {
                continue;
            }

            rows.Add(new RateRow(
                symbol,
                reader.GetDouble(1),
                reader.GetDouble(2),
                reader.GetInt64(3)));
        }

        return rows;
    }
}
