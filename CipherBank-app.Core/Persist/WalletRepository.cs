// <copyright file="WalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class WalletRepository : IWalletRepository
{
    private readonly ILocalDb _db;

    public WalletRepository(ILocalDb db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<LocalWalletRow>> ListAsync()
        => ListAsync(CancellationToken.None);

    public async Task<IReadOnlyList<LocalWalletRow>> ListAsync(CancellationToken ct)
    {
        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            return await context.Wallets
                .AsNoTracking()
                .OrderBy(entity => entity.CreatedAt)
                .Select(entity => new LocalWalletRow(entity))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
    }

    public Task UpsertAsync(LocalWalletRow row)
        => UpsertAsync(row, CancellationToken.None);

    public Task UpsertAsync(LocalWalletRow row, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(row);
        return UpsertCoreAsync(row, ct);
    }

    public Task DeleteAsync(string id) => DeleteAsync(id, CancellationToken.None);

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            WalletEntity? entity = await context.Wallets.FindAsync([id], ct).ConfigureAwait(false);
            if (entity is null)
            {
                return;
            }

            context.Wallets.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task UpsertCoreAsync(LocalWalletRow row, CancellationToken ct)
    {
        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            WalletEntity? entity = await context.Wallets.FindAsync([row.Id], ct).ConfigureAwait(false);
            if (entity is null)
            {
                entity = new WalletEntity { Id = row.Id, CreatedAt = row.CreatedAt };
                context.Wallets.Add(entity);
            }

            // Copies the matching mutable columns in one call; Id and CreatedAt stay insert-owned.
            context.Entry(entity).CurrentValues.SetValues(new
            {
                Symbol = row.Symbol.Value,
                row.Label,
                row.Address,
                row.Path,
                row.AccountIndex,
                row.Kind,
            });
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
