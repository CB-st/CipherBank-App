// <copyright file="IDeleteById.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Role seam for cancelable delete-by-id. Deleting a missing id is a no-op.
/// </summary>
public interface IDeleteById
{
    /// <summary>
    /// Deletes the row with <paramref name="id"/> when it exists.
    /// Use: Medium (editors). Scope: composing port's consumers.
    /// </summary>
    Task DeleteAsync(string id) => DeleteAsync(id, CancellationToken.None);

    Task DeleteAsync(string id, CancellationToken ct);
}
