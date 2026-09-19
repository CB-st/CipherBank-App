// <copyright file="IRateSnapshotStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>Stores the latest USD market rates.</summary>
public interface IRateSnapshotStore
{
    /// <summary>
    /// Writes the supplied USD rate rows, replacing any existing row for the same symbol.
    /// Use: High (quote hydrate). Scope: IRateSnapshotStore consumers.
    /// </summary>
    Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct);

    /// <summary>
    /// Returns cached USD rows. A null or empty <paramref name="symbols"/> set returns every row.
    /// Use: High (quote hydrate / home rates). Scope: IRateSnapshotStore consumers.
    /// </summary>
    Task<IReadOnlyList<RateRow>> GetAsync(
        IEnumerable<AssetSymbol>? symbols,
        CancellationToken ct);
}
