// <copyright file="AccountBootstrapStateStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class AccountBootstrapStateStore : IAccountBootstrapStateStore
{
    private readonly IDbContextFactory<CipherBankDbContext> _contexts;
    private readonly Action<int>? _recipientStaged;
    private readonly Action? _afterSaveChanges;

    /// <summary>Initializes a new instance of the <see cref="AccountBootstrapStateStore"/> class.</summary>
    public AccountBootstrapStateStore(IDbContextFactory<CipherBankDbContext> contexts)
        : this(contexts, null)
    {
    }

    internal AccountBootstrapStateStore(
        IDbContextFactory<CipherBankDbContext> contexts,
        Action<int>? recipientStaged,
        Action? afterSaveChanges = null)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        _contexts = contexts;
        _recipientStaged = recipientStaged;
        _afterSaveChanges = afterSaveChanges;
    }

    /// <inheritdoc />
    public async Task CommitAsync(UserPrefs prefs, IReadOnlyList<AchRecipientRow> recipients, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        ArgumentNullException.ThrowIfNull(recipients);

        CipherBankDbContext context = await _contexts.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using (context)
        {
            IDbContextTransaction transaction = await context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, ct)
                .ConfigureAwait(false);
            await using (transaction)
            {
                await PrefsStore.ApplyAsync(context, prefs, ct).ConfigureAwait(false);
                for (int index = 0; index < recipients.Count; index++)
                {
                    await RecipientEntityWriter.ApplyAsync(context, recipients[index], ct).ConfigureAwait(false);
                    _recipientStaged?.Invoke(index + 1);
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                _afterSaveChanges?.Invoke();
                await transaction.CommitAsync(ct).ConfigureAwait(false);
            }
        }
    }
}
