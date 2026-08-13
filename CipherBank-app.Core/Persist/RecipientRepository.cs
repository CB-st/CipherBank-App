// <copyright file="RecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RecipientRepository : IRecipientRepository
{
    private const string DefaultAccountType = "checking";

    private readonly ILocalDb _db;
    private readonly TimeProvider _timeProvider;

    public RecipientRepository(ILocalDb db)
        : this(db, TimeProvider.System)
    {
    }

    public RecipientRepository(ILocalDb db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task EnsureSchemaAsync() => _db.InitializeAsync();

    public async Task<IReadOnlyList<AchRecipientRow>> ListAsync()
    {
        await using CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        return await context.Recipients
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => new AchRecipientRow(
                entity.Id,
                entity.Name,
                entity.Holder,
                entity.Bank,
                Routing: null,
                Account: null,
                entity.AccountType,
                entity.Memo,
                entity.AccountMask,
                entity.RoutingMask,
                entity.CreatedAt))
            .ToListAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Upserts payee metadata and masks only; cleartext account/routing inputs never enter the EF model.
    /// </summary>
    public Task UpsertAsync(AchRecipientRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return UpsertCoreAsync(row);
    }

    public async Task DeleteAsync(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        await using CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        RecipientEntity? entity = await context.Recipients.FindAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            return;
        }

        context.Recipients.Remove(entity);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SeedDefaultsIfEmptyAsync()
    {
        await using (CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false))
        {
            if (await context.Recipients.AnyAsync().ConfigureAwait(false))
            {
                return;
            }
        }

        await UpsertAsync(new AchRecipientRow(
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            "Rent — 4th St LLC",
            "4th St LLC",
            "Demo Bank",
            "021000021",
            "88210001",
            DefaultAccountType,
            "Rent",
            null,
            null,
            _timeProvider.GetUtcNow())).ConfigureAwait(false);
        await UpsertAsync(new AchRecipientRow(
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            "Utilities Co",
            "Utilities Co",
            "City Credit Union",
            "110000000",
            "44102222",
            DefaultAccountType,
            null,
            null,
            null,
            _timeProvider.GetUtcNow())).ConfigureAwait(false);
    }

    private async Task UpsertCoreAsync(AchRecipientRow row)
    {
        // Prefer fresh cleartext: editing a listed row still carries prior masks.
        string? accountMask = string.IsNullOrWhiteSpace(row.Account)
            ? row.AccountMask
            : AchRecipientValidation.MaskAccount(row.Account);
        string? routingMask = string.IsNullOrWhiteSpace(row.Routing)
            ? row.RoutingMask
            : AchRecipientValidation.MaskRouting(row.Routing);

        await using CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        RecipientEntity? entity = await context.Recipients.FindAsync(row.Id).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new RecipientEntity { Id = row.Id, CreatedAt = row.CreatedAt };
            context.Recipients.Add(entity);
        }

        entity.Name = row.Name;
        entity.Holder = row.Holder;
        entity.Bank = row.Bank;
        entity.AccountType = string.IsNullOrWhiteSpace(row.AccountType)
            ? DefaultAccountType
            : row.AccountType;
        entity.Memo = row.Memo;
        entity.AccountMask = accountMask;
        entity.RoutingMask = routingMask;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
