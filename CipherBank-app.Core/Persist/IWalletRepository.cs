// <copyright file="IWalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>
/// SQLite wallets repo. Port invariants: rows carry address/path metadata only — key
/// material never enters this store — lists return wallets ordered by creation time, and
/// deleting a missing id is a no-op.
/// </summary>
public interface IWalletRepository
{
    /// <summary>
    /// Lists stored rows.
    /// Use: High (list surfaces). Scope: composing port's consumers.
    /// </summary>
    Task<IReadOnlyList<LocalWalletDescriptor>> ListAsync() => ListAsync(CancellationToken.None);

    Task<IReadOnlyList<LocalWalletDescriptor>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Inserts or replaces a row by its stable id.
    /// Use: High (save paths). Scope: composing port's consumers.
    /// </summary>
    Task UpsertAsync(LocalWalletDescriptor row) => UpsertAsync(row, CancellationToken.None);

    Task UpsertAsync(LocalWalletDescriptor row, CancellationToken ct);

    /// <summary>
    /// Deletes the row with <paramref name="id"/> when it exists.
    /// Use: Medium (editors). Scope: composing port's consumers.
    /// </summary>
    Task DeleteAsync(string id) => DeleteAsync(id, CancellationToken.None);

    Task DeleteAsync(string id, CancellationToken ct);
}
