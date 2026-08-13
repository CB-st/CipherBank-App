// <copyright file="RatesCache.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RatesCache : IRatesCache
{
    private readonly ILocalDb _db;

    public RatesCache(ILocalDb db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return UpsertCoreAsync(rows, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RateRow>> GetAsync(
        IEnumerable<string>? symbols,
        CancellationToken ct)
    {
        string[] requestedSymbols = symbols?
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        await using CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        IQueryable<RateSnapshotEntity> query = context.RateSnapshots.AsNoTracking();
        if (requestedSymbols.Length > 0)
        {
            query = query.Where(entity => requestedSymbols.Contains(entity.Symbol));
        }

        return await query
            .OrderBy(entity => entity.Symbol)
            .Select(entity => new RateRow(
                entity.Symbol,
                entity.Usd,
                entity.Change24H,
                entity.UpdatedAtMs))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task UpsertCoreAsync(IEnumerable<RateRow> rows, CancellationToken ct)
    {
        RateRow[] normalized = rows
            .Select(row => row with { Symbol = row.Symbol.ToUpperInvariant() })
            .GroupBy(row => row.Symbol, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToArray();
        if (normalized.Length == 0)
        {
            return;
        }

        await using CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        string[] symbols = normalized.Select(row => row.Symbol).ToArray();
        List<RateSnapshotEntity> existingRows = await context.RateSnapshots
            .Where(entity => symbols.Contains(entity.Symbol))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        Dictionary<string, RateSnapshotEntity> existing = existingRows.ToDictionary(
            entity => entity.Symbol,
            StringComparer.Ordinal);

        foreach (RateRow row in normalized)
        {
            if (!existing.TryGetValue(row.Symbol, out RateSnapshotEntity? entity))
            {
                entity = new RateSnapshotEntity { Symbol = row.Symbol };
                context.RateSnapshots.Add(entity);
            }

            entity.Usd = row.Usd;
            entity.Change24H = row.Change24h;
            entity.UpdatedAtMs = row.UpdatedAtMs;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
