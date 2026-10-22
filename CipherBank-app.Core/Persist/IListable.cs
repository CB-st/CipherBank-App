// <copyright file="IListable.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Role seam for cancelable row listing. Ordering and row-shape invariants (for example
/// mask-only recipient rows) are documented on the composing port.
/// </summary>
/// <typeparam name="TRow">Immutable row type returned to consumers.</typeparam>
public interface IListable<TRow>
{
    /// <summary>
    /// Lists stored rows.
    /// Use: High (list surfaces). Scope: composing port's consumers.
    /// </summary>
    Task<IReadOnlyList<TRow>> ListAsync() => ListAsync(CancellationToken.None);

    Task<IReadOnlyList<TRow>> ListAsync(CancellationToken ct);
}
