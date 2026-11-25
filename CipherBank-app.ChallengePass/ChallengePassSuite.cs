// <copyright file="ChallengePassSuite.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>Composed, named suite: one plugged algorithm + template + structure.</summary>
public sealed class ChallengePassSuite(
    string suiteId,
    ISealAlgorithm algorithm,
    IChallengeTemplate template,
    IChallengePassStructure structure)
{
    public string SuiteId { get; } = suiteId;

    public ISealAlgorithm Algorithm { get; } = algorithm;

    public IChallengeTemplate Template { get; } = template;

    public IChallengePassStructure Structure { get; } = structure;
}
