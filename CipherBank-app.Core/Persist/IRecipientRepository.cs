// <copyright file="IRecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite ACH recipients repo (Cora recipientsRepo).</summary>
public interface IRecipientRepository
{
    /// <summary>
    /// Ensures the persist schema exists before recipient reads or writes.
    /// Use: High (first payee list). Scope: IRecipientRepository consumers.
    /// </summary>
    Task EnsureSchemaAsync();

    /// <summary>
    /// Lists stored payees as mask-only rows (no account or routing cleartext).
    /// Use: High (payee picker). Scope: IRecipientRepository consumers.
    /// </summary>
    Task<IReadOnlyList<AchRecipientRow>> ListAsync();

    /// <summary>
    /// Upserts payee metadata and masks. Cleartext account/routing inputs never enter the EF model.
    /// Use: High (payee save). Scope: IRecipientRepository consumers.
    /// </summary>
    Task UpsertAsync(AchRecipientRow row);

    /// <summary>
    /// Deletes the payee with <paramref name="id"/> when it exists.
    /// Use: Medium (payee editor). Scope: IRecipientRepository consumers.
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Inserts the two default demo payees in one transaction when the table is empty.
    /// Use: High (first-run hydration). Scope: IRecipientRepository consumers.
    /// </summary>
    Task SeedDefaultsIfEmptyAsync();
}
