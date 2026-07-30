// <copyright file="MarketRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class MarketRepository : IMarketRepository
{
    private readonly ILocalDb _db;

    public MarketRepository(ILocalDb db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task UpsertOhlcAsync(
        string symbol,
        IEnumerable<(long T, double V)> points,
        CancellationToken ct)
    {
        var normalizedSymbol = symbol.ToUpperInvariant();
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = (Microsoft.Data.Sqlite.SqliteTransaction)await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        foreach ((var timestamp, var value) in points)
        {
            await using SqliteCommand cmd = conn.CreateCommand();
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
    public Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(
        string symbol,
        CancellationToken ct)
        => GetOhlcCoreAsync(symbol, null, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(
        string symbol,
        long fromT,
        CancellationToken ct)
        => GetOhlcCoreAsync(symbol, fromT, ct);

    private async Task<IReadOnlyList<(long T, double V)>> GetOhlcCoreAsync(
        string symbol,
        long? fromT,
        CancellationToken ct)
    {
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT t, v
            FROM ohlc
            WHERE symbol=$symbol AND ($fromT IS NULL OR t >= $fromT)
            ORDER BY t
            """;
        cmd.Parameters.AddWithValue("$symbol", symbol.ToUpperInvariant());
        cmd.Parameters.AddWithValue("$fromT", (object?)fromT ?? DBNull.Value);

        var points = new List<(long T, double V)>();
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            points.Add((reader.GetInt64(0), reader.GetDouble(1)));
        }

        return points;
    }
}
