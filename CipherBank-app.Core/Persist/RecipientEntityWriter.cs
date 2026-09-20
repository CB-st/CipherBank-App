// <copyright file="RecipientEntityWriter.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist.Entities;

namespace CipherBank_app.Persist;

/// <summary>Maps cleartext recipient input to mask-only persistence entities.</summary>
internal static class RecipientEntityWriter
{
    internal const string DefaultAccountType = "checking";

    /// <summary>
    /// Applies one recipient row while ensuring cleartext account coordinates never enter the EF model.
    /// Use: High (recipient writes and bootstrap). Scope: Persist assembly.
    /// </summary>
    internal static async Task ApplyAsync(
        CipherBankDbContext context,
        AchRecipientRow row,
        CancellationToken ct)
    {
        string? accountMask = string.IsNullOrWhiteSpace(row.Account)
            ? row.AccountMask
            : AchRecipientValidation.MaskAccount(row.Account);
        string? routingMask = string.IsNullOrWhiteSpace(row.Routing)
            ? row.RoutingMask
            : AchRecipientValidation.MaskRouting(row.Routing);

        RecipientEntity? entity = await context.Recipients.FindAsync([row.Id], ct).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new RecipientEntity { Id = row.Id, CreatedAt = row.CreatedAt };
            context.Recipients.Add(entity);
        }

        // Copies the mutable columns in one call; Id and CreatedAt stay insert-owned.
        context.Entry(entity).CurrentValues.SetValues(new
        {
            row.Name,
            row.Holder,
            row.Bank,
            AccountType = string.IsNullOrWhiteSpace(row.AccountType)
                ? DefaultAccountType
                : row.AccountType,
            row.Memo,
            AccountMask = accountMask,
            RoutingMask = routingMask,
        });
    }
}
