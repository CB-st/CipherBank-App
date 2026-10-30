// <copyright file="WalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class WalletRepository : IWalletRepository
{
    private readonly IDbContextFactory<CipherBankDbContext> _contexts;

    public WalletRepository(IDbContextFactory<CipherBankDbContext> contexts)
    {
        _contexts = contexts;
    }

    public Task<IReadOnlyList<LocalWalletDescriptor>> ListAsync()
        => ListAsync(CancellationToken.None);

    public async Task<IReadOnlyList<LocalWalletDescriptor>> ListAsync(CancellationToken ct)
    {
        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            List<WalletEntity> entities = await context.Wallets
                .AsNoTracking()
                .OrderBy(entity => entity.CreatedAt)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return entities.Select(static entity => entity.ToDescriptor()).ToList();
        }
    }

    public Task UpsertAsync(LocalWalletDescriptor row)
        => UpsertAsync(row, CancellationToken.None);

    public Task UpsertAsync(LocalWalletDescriptor row, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(row);
        return UpsertCoreAsync(row, ct);
    }

    public Task DeleteAsync(string id) => DeleteAsync(id, CancellationToken.None);

    public async Task DeleteAsync(string id, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
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

    private async Task UpsertCoreAsync(LocalWalletDescriptor row, CancellationToken ct)
    {
        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
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
