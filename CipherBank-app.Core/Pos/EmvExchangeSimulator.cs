// <copyright file="EmvExchangeSimulator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Runtime.CompilerServices;

namespace CipherBank_app.Pos;

/// <summary>Simulated EMV exchange stages for PosLab UI.</summary>
public sealed class EmvExchangeSimulator : IEmvExchangeSimulator
{
    private const int StageDelayMs = 400;

    public IReadOnlyList<string> Stages { get; } =
    [
        "SELECT PPSE",
        "SELECT AID",
        "GET PROCESSING OPTIONS",
        "GENERATE AC",
        "OUTCOME: APPROVED",
    ];

    /// <inheritdoc />
    public IAsyncEnumerable<string> RunAsync() => RunAsync(CancellationToken.None);

    /// <inheritdoc />
    public async IAsyncEnumerable<string> RunAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        foreach (string stage in Stages)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(StageDelayMs, ct).ConfigureAwait(false);
            yield return stage;
        }
    }
}
