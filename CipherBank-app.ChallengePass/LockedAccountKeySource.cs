// <copyright file="LockedAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Placeholder until custody-backed <see cref="IAccountKeySource"/> is wired.
/// </summary>
public sealed class LockedAccountKeySource : IAccountKeySource
{
    public AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm)
        => throw new InvalidOperationException(
            "Account key source is not unlocked. Wire custody-backed IAccountKeySource before enabling ChallengePassSessionProofBuilder.");

    public HybridPrivateIdentity RequireHybridIdentity()
        => throw new InvalidOperationException(
            "Account key source is not unlocked. Wire custody-backed IAccountKeySource before enabling ChallengePassSessionProofBuilder.");
}
