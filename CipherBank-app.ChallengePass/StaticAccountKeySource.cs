// <copyright file="StaticAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;

namespace CipherBank_app.ChallengePass;

/// <summary>Test/lab account key source with a fixed in-memory keypair / hybrid identity.</summary>
public sealed class StaticAccountKeySource : IAccountKeySource
{
    private readonly AccountKeyPair _pair;
    private readonly HybridPrivateIdentity? _hybrid;

    public StaticAccountKeySource(AccountKeyPair pair, HybridPrivateIdentity? hybrid = null)
    {
        _pair = pair;
        _hybrid = hybrid;
    }

    public AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm) => _pair;

    public HybridPrivateIdentity RequireHybridIdentity()
        => _hybrid ?? throw new InvalidOperationException("No hybrid identity configured on StaticAccountKeySource.");
}
