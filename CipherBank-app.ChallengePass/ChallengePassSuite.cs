// <copyright file="ChallengePassSuite.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>Composed, named suite: one plugged algorithm + template + structure.</summary>
public sealed class ChallengePassSuite
{
    public ChallengePassSuite(
        string suiteId,
        ISealAlgorithm algorithm,
        IChallengeTemplate template,
        IChallengePassStructure structure)
    {
        SuiteId = suiteId;
        Algorithm = algorithm;
        Template = template;
        Structure = structure;
    }

    public string SuiteId { get; }

    public ISealAlgorithm Algorithm { get; }

    public IChallengeTemplate Template { get; }

    public IChallengePassStructure Structure { get; }
}
