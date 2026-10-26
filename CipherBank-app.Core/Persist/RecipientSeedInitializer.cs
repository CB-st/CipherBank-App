// <copyright file="RecipientSeedInitializer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Data;
using CipherBank_app.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RecipientSeedInitializer : IRecipientSeedInitializer
{
    private readonly IDbContextFactory<CipherBankDbContext> _contexts;
    private readonly PersistenceOptions _options;
    private readonly TimeProvider _timeProvider;

    public RecipientSeedInitializer(
        IDbContextFactory<CipherBankDbContext> contexts,
        IOptions<PersistenceOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (!options.Value.IsValid())
        {
            throw new ArgumentException(@"Persistence options are invalid.", nameof(options));
        }

        _contexts = contexts;
        _options = options.Value;
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

        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
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
