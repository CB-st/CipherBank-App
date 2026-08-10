// <copyright file="MarketRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(points);
        var normalizedSymbol = symbol.ToUpperInvariant();
        var latestByTimestamp = points
            .GroupBy(point => point.T)
            .ToDictionary(group => group.Key, group => group.Last().V);
        if (latestByTimestamp.Count == 0)
        {
            return;
        }

        await using var context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        var timestamps = latestByTimestamp.Keys.ToArray();
        var existing = await context.OhlcPoints
            .Where(entity => entity.Symbol == normalizedSymbol && timestamps.Contains(entity.Timestamp))
            .ToDictionaryAsync(entity => entity.Timestamp, ct)
            .ConfigureAwait(false);

        foreach (var point in latestByTimestamp)
        {
            if (!existing.TryGetValue(point.Key, out var entity))
            {
                entity = new OhlcPointEntity
                {
                    Symbol = normalizedSymbol,
                    Timestamp = point.Key,
                };
                context.OhlcPoints.Add(entity);
            }

            entity.Value = point.Value;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var normalizedSymbol = symbol.ToUpperInvariant();
        await using var context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        IQueryable<OhlcPointEntity> query = context.OhlcPoints
            .AsNoTracking()
            .Where(entity => entity.Symbol == normalizedSymbol);
        if (fromT.HasValue)
        {
            query = query.Where(entity => entity.Timestamp >= fromT.Value);
        }

        var entities = await query
            .OrderBy(entity => entity.Timestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return entities.Select(entity => (entity.Timestamp, entity.Value)).ToList();
    }
}
