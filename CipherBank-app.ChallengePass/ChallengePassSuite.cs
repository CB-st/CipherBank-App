// <copyright file="ChallengePassSuite.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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
