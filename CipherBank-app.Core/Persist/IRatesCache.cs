// <copyright file="IRatesCache.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Stores the latest USD market rates.</summary>
public interface IRatesCache
{
    Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct);

    Task<IReadOnlyList<RateRow>> GetAsync(
        IEnumerable<string>? symbols,
        CancellationToken ct);
}
