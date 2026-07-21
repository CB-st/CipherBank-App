// <copyright file="IMarketRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Stores market history points.</summary>
public interface IMarketRepository
{
    Task UpsertOhlcAsync(
        string symbol,
        IEnumerable<(long T, double V)> points,
        CancellationToken ct = default);

    Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(
        string symbol,
        long? fromT = null,
        CancellationToken ct = default);
}
