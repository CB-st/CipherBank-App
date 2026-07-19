// <copyright file="LockedAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Placeholder until custody-backed <see cref="IAccountKeySource"/> is wired.
/// Keeps DI complete while lab proof builder remains the active session opener.
/// </summary>
public sealed class LockedAccountKeySource : IAccountKeySource
{
    public AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm)
        => throw new InvalidOperationException(
            "Account key source is not unlocked. Wire custody-backed IAccountKeySource before enabling ChallengePassSessionProofBuilder.");
}
