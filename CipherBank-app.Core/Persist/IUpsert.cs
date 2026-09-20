// <copyright file="IUpsert.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Role seam for cancelable keyed insert-or-replace writes. Identity and sanitation
/// invariants are documented on the composing port.
/// </summary>
/// <typeparam name="TRow">Immutable row type accepted from consumers (contravariant).</typeparam>
public interface IUpsert<in TRow>
{
    /// <summary>
    /// Inserts or replaces a row by its stable id.
    /// Use: High (save paths). Scope: composing port's consumers.
    /// </summary>
    Task UpsertAsync(TRow row) => UpsertAsync(row, CancellationToken.None);

    Task UpsertAsync(TRow row, CancellationToken ct);
}
