// <copyright file="WalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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

    public async Task<IReadOnlyList<LocalWalletRow>> ListAsync()
    {
        await using var context = await _db.CreateContextAsync().ConfigureAwait(false);
        return await context.Wallets
            .AsNoTracking()
            .OrderBy(entity => entity.CreatedAt)
            .Select(entity => new LocalWalletRow(
                entity.Id,
                entity.Symbol,
                entity.Label,
                entity.Address,
                entity.Path,
                entity.AccountIndex,
                entity.Kind,
                entity.CreatedAt))
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task UpsertAsync(LocalWalletRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        await using var context = await _db.CreateContextAsync().ConfigureAwait(false);
        var entity = await context.Wallets.FindAsync(row.Id).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new WalletEntity { Id = row.Id, CreatedAt = row.CreatedAt };
            context.Wallets.Add(entity);
        }

        entity.Symbol = row.Symbol;
        entity.Label = row.Label;
        entity.Address = row.Address;
        entity.Path = row.Path;
        entity.AccountIndex = row.AccountIndex;
        entity.Kind = row.Kind;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await using var context = await _db.CreateContextAsync().ConfigureAwait(false);
        var entity = await context.Wallets.FindAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            return;
        }

        context.Wallets.Remove(entity);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
