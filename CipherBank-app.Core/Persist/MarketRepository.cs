// <copyright file="MarketRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class MarketRepository : IMarketRepository
{
    private readonly ILocalDb _db;

    public MarketRepository(ILocalDb db) => _db = db;

    /// <inheritdoc />
    public async Task UpsertOhlcAsync(
        string symbol,
        IEnumerable<(long T, double V)> points,
        CancellationToken ct = default)
    {
        string normalizedSymbol = symbol.ToUpperInvariant();
        await using var conn = _db.Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = conn.BeginTransaction();
        foreach ((long timestamp, double value) in points)
        {
            await using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO ohlc (symbol, t, v)
                VALUES ($symbol, $timestamp, $value)
                ON CONFLICT(symbol, t) DO UPDATE SET v=$value
                """;
            cmd.Parameters.AddWithValue("$symbol", normalizedSymbol);
            cmd.Parameters.AddWithValue("$timestamp", timestamp);
            cmd.Parameters.AddWithValue("$value", value);
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(
        string symbol,
        long? fromT = null,
        CancellationToken ct = default)
    {
        await using var conn = _db.Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t, v
            FROM ohlc
            WHERE symbol=$symbol AND ($fromT IS NULL OR t >= $fromT)
            ORDER BY t
            """;
        cmd.Parameters.AddWithValue("$symbol", symbol.ToUpperInvariant());
        cmd.Parameters.AddWithValue("$fromT", (object?)fromT ?? DBNull.Value);

        var points = new List<(long T, double V)>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            points.Add((reader.GetInt64(0), reader.GetDouble(1)));
        }

        return points;
    }
}
