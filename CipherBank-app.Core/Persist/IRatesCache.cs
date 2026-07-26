// <copyright file="IRatesCache.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>A cached market rate.</summary>
public sealed record RateRow(string Symbol, double Usd, double Change24h, long UpdatedAtMs);

/// <summary>Stores the latest USD market rates.</summary>
public interface IRatesCache
{
    Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct);

    Task<IReadOnlyList<RateRow>> GetAsync(
        IEnumerable<string>? symbols,
        CancellationToken ct);
}
