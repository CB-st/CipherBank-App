// <copyright file="IEmvExchangeSimulator.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Pos;

/// <summary>Provides a development-only EMV exchange sequence.</summary>
public interface IEmvExchangeSimulator
{
    IReadOnlyList<string> Stages { get; }

    IAsyncEnumerable<string> RunAsync(CancellationToken ct = default);
}
