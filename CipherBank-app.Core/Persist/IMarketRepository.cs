// <copyright file="IMarketRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>Stores market history points.</summary>
public interface IMarketRepository
{
    /// <summary>
    /// Replaces stored value-series points for <paramref name="symbol"/>.
    /// Use: High (chart persist). Scope: IMarketRepository consumers.
    /// </summary>
    Task UpsertOhlcAsync(
        AssetSymbol symbol,
        IEnumerable<PricePoint> points,
        CancellationToken ct);

    /// <summary>
    /// Returns the full stored series for <paramref name="symbol"/>, oldest first.
    /// Use: High (chart load). Scope: IMarketRepository consumers.
    /// </summary>
    Task<IReadOnlyList<PricePoint>> GetOhlcAsync(
        AssetSymbol symbol,
        CancellationToken ct);

    /// <summary>
    /// Returns stored series points at or after <paramref name="fromT"/> for <paramref name="symbol"/>.
    /// Use: Medium (chart window). Scope: IMarketRepository consumers.
    /// </summary>
    Task<IReadOnlyList<PricePoint>> GetOhlcAsync(
        AssetSymbol symbol,
        long fromT,
        CancellationToken ct);
}
