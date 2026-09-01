// <copyright file="RecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Data;
using CipherBank_app.Configuration;
using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RecipientRepository : IRecipientRepository
{
    /// <summary>
    /// Stable seed primary key for the demo rent payee. Must match Persistence:DefaultRecipients.
    /// </summary>
    internal const string DefaultRentRecipientId = "seed:rent-4th-st";

    /// <summary>
    /// Stable seed primary key for the demo utilities payee. Must match Persistence:DefaultRecipients.
    /// </summary>
    internal const string DefaultUtilitiesRecipientId = "seed:utilities-co";

    private static readonly string DefaultAccountType = "checking";

    private readonly ILocalDb _db;
    private readonly PersistenceOptions _options;
    private readonly TimeProvider _timeProvider;

    public RecipientRepository(ILocalDb db, PersistenceOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(options);
        if (!options.AreDefaultRecipientsValid())
        {
            throw new ArgumentException("DefaultRecipients must have unique non-blank ids and names.", nameof(options));
        }

        _db = db;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task EnsureSchemaAsync() => _db.InitializeAsync();

    public async Task<IReadOnlyList<AchRecipientRow>> ListAsync()
    {
        CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        await using (context)
        {
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
        CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        await using (context)
        {
            RecipientEntity? entity = await context.Recipients.FindAsync(id).ConfigureAwait(false);
            if (entity is null)
            {
                return;
            }

            context.Recipients.Remove(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Inserts configured default payees in one transaction when the table is empty.
    /// Use: High (first-run and concurrent hydration). Scope: RecipientRepository.
    /// </summary>
    public async Task SeedDefaultsIfEmptyAsync()
    {
        if (_options.DefaultRecipients.Count == 0)
        {
            return;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        await using (context)
        {
            IDbContextTransaction transaction = await context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable)
                .ConfigureAwait(false);
            await using (transaction)
            {
                if (await context.Recipients.AnyAsync().ConfigureAwait(false))
                {
                    return;
                }

                foreach (DefaultRecipientOptions seed in _options.DefaultRecipients)
                {
                    await ApplyRowAsync(
                        context,
                        new AchRecipientRow(
                            seed.Id,
                            seed.Name,
                            seed.Holder,
                            seed.Bank,
                            seed.Routing,
                            seed.Account,
                            string.IsNullOrWhiteSpace(seed.AccountType) ? DefaultAccountType : seed.AccountType,
                            seed.Memo,
                            null,
                            null,
                            now)).ConfigureAwait(false);
                }

                await context.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task ApplyRowAsync(CipherBankDbContext context, AchRecipientRow row)
    {
        // Prefer fresh cleartext: editing a listed row still carries prior masks.
        string? accountMask = string.IsNullOrWhiteSpace(row.Account)
            ? row.AccountMask
            : AchRecipientValidation.MaskAccount(row.Account);
        string? routingMask = string.IsNullOrWhiteSpace(row.Routing)
            ? row.RoutingMask
            : AchRecipientValidation.MaskRouting(row.Routing);

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
    }

    private async Task UpsertCoreAsync(AchRecipientRow row)
    {
        CipherBankDbContext context = await _db.CreateContextAsync().ConfigureAwait(false);
        await using (context)
        {
            await ApplyRowAsync(context, row).ConfigureAwait(false);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
