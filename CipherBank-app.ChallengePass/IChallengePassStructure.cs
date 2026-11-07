// <copyright file="IChallengePassStructure.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Slot 3 — protocol structure (round-trips, request/response mapping).
/// Swap to change challenge→pass HTTP choreography without changing crypto or framing.
/// </summary>
public interface IChallengePassStructure
{
    string StructureId { get; }

    Task<object> BuildSessionOpenBodyAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire);

    Task<object> BuildSessionOpenBodyAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire,
        CancellationToken ct);
}
