// <copyright file="IRatesCache.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Stores the latest USD market rates.</summary>
public interface IRatesCache
{
    /// <summary>
    /// Writes the supplied USD rate rows, replacing any existing row for the same symbol.
    /// Use: High (quote hydrate). Scope: IRatesCache consumers.
    /// </summary>
    Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct);

    /// <summary>
    /// Returns cached USD rows. A null or empty <paramref name="symbols"/> set returns every row.
    /// Use: High (quote hydrate / home rates). Scope: IRatesCache consumers.
    /// </summary>
    Task<IReadOnlyList<RateRow>> GetAsync(
        IEnumerable<string>? symbols,
        CancellationToken ct);
}
