// <copyright file="MarketRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class MarketRepository : IMarketRepository
{
    private readonly IDbContextFactory<CipherBankDbContext> _contexts;

    public MarketRepository(IDbContextFactory<CipherBankDbContext> contexts)
    {
        _contexts = contexts;
    }

    /// <inheritdoc />
    public Task UpsertOhlcAsync(
        AssetSymbol symbol,
        IEnumerable<PricePoint> points,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(points);
        return UpsertOhlcCoreAsync(symbol, points, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PricePoint>> GetOhlcAsync(
        AssetSymbol symbol,
        CancellationToken ct)
        => GetOhlcCoreAsync(symbol, null, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<PricePoint>> GetOhlcAsync(
        AssetSymbol symbol,
        long fromT,
        CancellationToken ct)
        => GetOhlcCoreAsync(symbol, fromT, ct);

    private async Task UpsertOhlcCoreAsync(
        AssetSymbol symbol,
        IEnumerable<PricePoint> points,
        CancellationToken ct)
    {
        string normalizedSymbol = symbol.Value;
        PricePoint[] snapshot = points.ToArray();
        if (snapshot is [])
        {
            return;
        }

        Dictionary<long, PricePoint> latestByTimestamp = snapshot
            .GroupBy(point => point.Timestamp.ToUnixTimeMilliseconds())
            .ToDictionary(group => group.Key, group => group.Last());

        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            long[] timestamps = latestByTimestamp.Keys.ToArray();
            Dictionary<long, OhlcPointEntity> existing = await context.OhlcPoints
                .Where(entity => entity.Symbol == normalizedSymbol && timestamps.Contains(entity.Timestamp))
                .ToDictionaryAsync(entity => entity.Timestamp, ct)
                .ConfigureAwait(false);

            foreach (KeyValuePair<long, PricePoint> point in latestByTimestamp)
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

                entity.Price = point.Value.Price;
                entity.Volume = point.Value.Volume;
            }

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private Task<IReadOnlyList<PricePoint>> GetOhlcCoreAsync(
        AssetSymbol symbol,
        long? fromT,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return GetOhlcFromDatabaseAsync(symbol.Value, fromT, ct);
    }

    private async Task<IReadOnlyList<PricePoint>> GetOhlcFromDatabaseAsync(
        string normalizedSymbol,
        long? fromT,
        CancellationToken ct)
    {
        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
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
            return entities.Select(entity => new PricePoint(
                DateTimeOffset.FromUnixTimeMilliseconds(entity.Timestamp),
                entity.Price,
                entity.Volume)).ToList();
        }
    }
}
