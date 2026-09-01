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
    public Task UpsertOhlcAsync(
        string symbol,
        IEnumerable<(long T, double V)> points,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentNullException.ThrowIfNull(points);
        return UpsertOhlcCoreAsync(symbol, points, ct);
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

    private async Task UpsertOhlcCoreAsync(
        string symbol,
        IEnumerable<(long T, double V)> points,
        CancellationToken ct)
    {
        string normalizedSymbol = symbol.ToUpperInvariant();
        Dictionary<long, double> latestByTimestamp = points
            .GroupBy(point => point.T)
            .ToDictionary(group => group.Key, group => group.Last().V);
        if (latestByTimestamp.Count == 0)
        {
            return;
        }

        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            long[] timestamps = latestByTimestamp.Keys.ToArray();
            Dictionary<long, OhlcPointEntity> existing = await context.OhlcPoints
                .Where(entity => entity.Symbol == normalizedSymbol && timestamps.Contains(entity.Timestamp))
                .ToDictionaryAsync(entity => entity.Timestamp, ct)
                .ConfigureAwait(false);

            foreach (KeyValuePair<long, double> point in latestByTimestamp)
            {
                if (!existing.TryGetValue(point.Key, out OhlcPointEntity? entity))
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
    }

    private async Task<IReadOnlyList<(long T, double V)>> GetOhlcCoreAsync(
        string symbol,
        long? fromT,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        string normalizedSymbol = symbol.ToUpperInvariant();
        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            IQueryable<OhlcPointEntity> query = context.OhlcPoints
                .AsNoTracking()
                .Where(entity => entity.Symbol == normalizedSymbol);
            if (fromT.HasValue)
            {
                query = query.Where(entity => entity.Timestamp >= fromT.Value);
            }

            List<OhlcPointEntity> entities = await query
                .OrderBy(entity => entity.Timestamp)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return entities.Select(entity => (entity.Timestamp, entity.Value)).ToList();
        }
    }
}
