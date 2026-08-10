// <copyright file="EmvExchangeSimulator.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Pos;

/// <summary>Simulated EMV exchange stages for PosLab UI.</summary>
public sealed class EmvExchangeSimulator : IEmvExchangeSimulator
{
    private const int StageDelayMs = 400;

    public IReadOnlyList<string> Stages { get; } = new[]
    {
        "SELECT PPSE",
        "SELECT AID",
        "GET PROCESSING OPTIONS",
        "GENERATE AC",
        "OUTCOME: APPROVED",
    };

    /// <summary>Streams the simulated stages for callers with no ambient token.</summary>
    /// <returns>Stage labels in exchange order.</returns>
    /// <remarks>Use: Low (PosLab simulate). Scope: PosLab view model.</remarks>
    public async IAsyncEnumerable<string> RunAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var stage in Stages)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(StageDelayMs, ct).ConfigureAwait(false);
            yield return stage;
        }
    }
}
