// <copyright file="RecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RecipientRepository : IRecipientRepository
{
    private readonly IDbContextFactory<CipherBankDbContext> _contexts;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecipientRepository"/> class.
    /// Use: Medium (host composition). Scope: application database.
    /// </summary>
    public RecipientRepository(IDbContextFactory<CipherBankDbContext> contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        _contexts = contexts;
    }

    public Task<IReadOnlyList<AchRecipientRow>> ListAsync()
        => ListAsync(CancellationToken.None);

    public async Task<IReadOnlyList<AchRecipientRow>> ListAsync(CancellationToken ct)
    {
        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            return await context.Recipients
                .AsNoTracking()
                .OrderBy(entity => entity.Name)
                .Select(entity => new AchRecipientRow(entity))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Upserts payee metadata and masks only; cleartext account/routing inputs never enter the EF model.
    /// </summary>
    public Task UpsertAsync(AchRecipientRow row)
        => UpsertAsync(row, CancellationToken.None);

    public Task UpsertAsync(AchRecipientRow row, CancellationToken ct)
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
            RecipientEntity? entity = await context.Recipients.FindAsync([id], ct).ConfigureAwait(false);
            if (entity is null)
            {
                return;
            }

            context.Recipients.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task UpsertCoreAsync(AchRecipientRow row, CancellationToken ct)
    {
        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            await RecipientEntityWriter.ApplyAsync(context, row, ct).ConfigureAwait(false);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
