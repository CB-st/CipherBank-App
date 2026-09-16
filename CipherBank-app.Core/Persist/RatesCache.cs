// <copyright file="RatesCache.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc cref="IRatesCache" />
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
        IEnumerable<AssetSymbol>? symbols,
        CancellationToken ct)
    {
        AssetSymbol[] requestedSymbols = symbols?
            .Distinct()
            .ToArray() ?? [];

        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            IQueryable<RateSnapshotEntity> query = context.RateSnapshots.AsNoTracking();
            if (requestedSymbols.Length > 0)
            {
                string[] requestedValues = requestedSymbols.Select(symbol => symbol.Value).ToArray();
                query = query.Where(entity => requestedValues.Contains(entity.Symbol));
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
            .GroupBy(row => row.Symbol)
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
                string[] symbols = normalized.Select(row => row.Symbol.Value).ToArray();
                Dictionary<string, RateSnapshotEntity> existing = await context.RateSnapshots
                    .Where(entity => symbols.Contains(entity.Symbol))
                    .ToDictionaryAsync(entity => entity.Symbol, StringComparer.Ordinal, ct)
                    .ConfigureAwait(false);

                foreach (RateRow row in normalized)
                {
                    bool found = existing.TryGetValue(row.Symbol.Value, out RateSnapshotEntity? entity);
                    if (found && row.UpdatedAtMs < entity!.UpdatedAtMs)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        entity = new RateSnapshotEntity { Symbol = row.Symbol.Value };
                        context.RateSnapshots.Add(entity);
                    }

                    // Copies the mutable columns in one call; Symbol stays insert-owned.
                    context.Entry(entity!).CurrentValues.SetValues(new
                    {
                        row.Usd,
                        row.Change24H,
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
