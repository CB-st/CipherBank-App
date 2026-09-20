// <copyright file="RatesCache.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RatesCache : IRatesCache, IDisposable
{
    private readonly ILocalDb _db;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public RatesCache(ILocalDb db)
    {
        _db = db;
    }

    /// <summary>
    /// Releases the write-serialization gate.
    /// Use: Low (container shutdown). Scope: RatesCache instance.
    /// </summary>
    public void Dispose() => _writeGate.Dispose();

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
            .Select(RateRow.NormalizeSymbol)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            IQueryable<RateSnapshotEntity> query = context.RateSnapshots.AsNoTracking();
            if (requestedSymbols.Length > 0)
            {
                query = query.Where(entity => requestedSymbols.Contains(entity.Symbol));
            }

            return await query
                .OrderBy(entity => entity.Symbol)
                .Select(entity => new RateRow(entity))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
    }

    private async Task UpsertCoreAsync(IEnumerable<RateRow> rows, CancellationToken ct)
    {
        RateRow[] normalized = rows
            .GroupBy(row => row.Symbol, StringComparer.Ordinal)
            .Select(group => group.MaxBy(row => row.UpdatedAtMs)!)
            .ToArray();
        if (normalized is [])
        {
            return;
        }

        await _writeGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
            await using (context)
            {
                string[] symbols = normalized.Select(row => row.Symbol).ToArray();
                Dictionary<string, RateSnapshotEntity> existing = await context.RateSnapshots
                    .Where(entity => symbols.Contains(entity.Symbol))
                    .ToDictionaryAsync(entity => entity.Symbol, StringComparer.Ordinal, ct)
                    .ConfigureAwait(false);

                foreach (RateRow row in normalized)
                {
                    bool found = existing.TryGetValue(row.Symbol, out RateSnapshotEntity? entity);
                    if (found && row.UpdatedAtMs < entity!.UpdatedAtMs)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        entity = new RateSnapshotEntity { Symbol = row.Symbol };
                        context.RateSnapshots.Add(entity);
                    }

                    // Copies the mutable columns in one call; Symbol stays insert-owned.
                    context.Entry(entity!).CurrentValues.SetValues(new
                    {
                        row.Usd,
                        Change24H = row.Change24h,
                        row.UpdatedAtMs,
                    });
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }
}
