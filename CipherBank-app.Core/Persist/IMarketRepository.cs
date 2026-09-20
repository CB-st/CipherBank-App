// <copyright file="IMarketRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Stores market history points.</summary>
public interface IMarketRepository
{
    /// <summary>
    /// Replaces stored value-series points for <paramref name="symbol"/>.
    /// Use: High (chart persist). Scope: IMarketRepository consumers.
    /// </summary>
    Task UpsertOhlcAsync(
        string symbol,
        IEnumerable<(long T, double V)> points,
        CancellationToken ct);

    /// <summary>
    /// Returns the full stored series for <paramref name="symbol"/>, oldest first.
    /// Use: High (chart load). Scope: IMarketRepository consumers.
    /// </summary>
    Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(
        string symbol,
        CancellationToken ct);

    /// <summary>
    /// Returns stored series points at or after <paramref name="fromT"/> for <paramref name="symbol"/>.
    /// Use: Medium (chart window). Scope: IMarketRepository consumers.
    /// </summary>
    Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(
        string symbol,
        long fromT,
        CancellationToken ct);
}
