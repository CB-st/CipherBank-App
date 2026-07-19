// <copyright file="StaticAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>Test/lab account key source with a fixed in-memory keypair.</summary>
public sealed class StaticAccountKeySource : IAccountKeySource
{
    private readonly AccountKeyPair _pair;

    public StaticAccountKeySource(AccountKeyPair pair) => _pair = pair;

    public AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm) => _pair;
}
