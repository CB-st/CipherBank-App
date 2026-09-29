// <copyright file="IRecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// SQLite ACH recipients repo (Cora recipientsRepo).
/// Port invariants: rows are mask-only (cleartext account/routing inputs never enter the
/// EF model), lists return payees in stored order for the picker, deleting a missing id
/// is a no-op, and the schema is ensured before the first payee read or write.
/// </summary>
public interface IRecipientRepository
{
    /// <summary>
    /// Lists stored rows.
    /// Use: High (list surfaces). Scope: composing port's consumers.
    /// </summary>
    Task<IReadOnlyList<AchRecipientRow>> ListAsync() => ListAsync(CancellationToken.None);

    Task<IReadOnlyList<AchRecipientRow>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Inserts or replaces a row by its stable id.
    /// Use: High (save paths). Scope: composing port's consumers.
    /// </summary>
    Task UpsertAsync(AchRecipientRow row) => UpsertAsync(row, CancellationToken.None);

    Task UpsertAsync(AchRecipientRow row, CancellationToken ct);

    /// <summary>
    /// Deletes the row with <paramref name="id"/> when it exists.
    /// Use: Medium (editors). Scope: composing port's consumers.
    /// </summary>
    Task DeleteAsync(string id) => DeleteAsync(id, CancellationToken.None);

    Task DeleteAsync(string id, CancellationToken ct);
}
