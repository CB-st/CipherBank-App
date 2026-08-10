// <copyright file="IAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;

namespace CipherBank_app.ChallengePass;

/// <summary>Provides unlocked account key material to the proof builder.</summary>
public interface IAccountKeySource
{
    AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm);

    /// <summary>Hybrid ML-KEM + X25519 identity for A2 key-share (custody unlocked).</summary>
    HybridPrivateIdentity RequireHybridIdentity();
}
