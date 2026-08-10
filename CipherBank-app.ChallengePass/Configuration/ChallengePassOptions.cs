// <copyright file="ChallengePassOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass.Configuration;

/// <summary>Non-secret selection settings for the installed challenge/pass suites.</summary>
public sealed class ChallengePassOptions
{
    public const string SectionName = "ChallengePass";

    public string ActiveSuiteId { get; set; } = ChallengePassServiceCollectionExtensions.SuiteA1Id;

    /// <summary>Returns whether the configured active suite is installed by this module.</summary>
    public bool IsValid()
        => string.Equals(
                ActiveSuiteId,
                ChallengePassServiceCollectionExtensions.SuiteA1Id,
                StringComparison.Ordinal)
            || string.Equals(
                ActiveSuiteId,
                ChallengePassServiceCollectionExtensions.SuiteA2Id,
                StringComparison.Ordinal);
}
