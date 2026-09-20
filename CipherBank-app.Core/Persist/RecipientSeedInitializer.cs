// <copyright file="RecipientSeedInitializer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Data;
using CipherBank_app.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RecipientSeedInitializer : IRecipientSeedInitializer
{
    private readonly ILocalDb _db;
    private readonly PersistenceOptions _options;
    private readonly TimeProvider _timeProvider;

    public RecipientSeedInitializer(
        ILocalDb db,
        PersistenceOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (!options.IsValid())
        {
            throw new ArgumentException("Persistence options are invalid.", nameof(options));
        }

        _db = db;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => InitializeAsync(CancellationToken.None);

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (_options.DefaultRecipients.Count == 0)
        {
            return;
        }

        CipherBankDbContext context = await _db.CreateContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            IDbContextTransaction transaction = await context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, ct)
                .ConfigureAwait(false);
            await using (transaction)
            {
                if (await context.Recipients.AnyAsync(ct).ConfigureAwait(false))
                {
                    return;
                }

                DateTimeOffset now = _timeProvider.GetUtcNow();
                foreach (DefaultRecipientOptions seed in _options.DefaultRecipients)
                {
                    await RecipientEntityWriter.ApplyAsync(
                        context,
                        new AchRecipientRow(seed, now),
                        ct).ConfigureAwait(false);
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
        }
    }
}
