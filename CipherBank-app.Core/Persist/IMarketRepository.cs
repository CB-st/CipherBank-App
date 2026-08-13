// <copyright file="IMarketRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Stores market history points.</summary>
public interface IMarketRepository
{
    Task UpsertOhlcAsync(
        string symbol,
        IEnumerable<(long T, double V)> points,
        CancellationToken ct);

    Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(
        string symbol,
        CancellationToken ct);

    Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(
        string symbol,
        long fromT,
        CancellationToken ct);
}
