// <copyright file="IAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Provides unlocked account key material to the proof builder.
/// Callers own every returned buffer and must <c>CryptographicOperations.ZeroMemory</c> private
/// material when finished. Sources that retain keys must hand out copies so caller wipes do not
/// destroy retained fixture or vault state.
/// </summary>
public interface IAccountKeySource
{
    /// <summary>
    /// Returns an unlocked A1 account key pair; caller owns the buffers.
    /// Use: High (A1 proof construction). Scope: unlocked custody or lab fixture.
    /// </summary>
    AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm);

    /// <summary>
    /// Returns hybrid ML-KEM + X25519 identity for A2 key-share; caller owns the buffers.
    /// Use: High (A2 proof construction). Scope: unlocked custody or lab fixture.
    /// </summary>
    HybridPrivateIdentity RequireHybridIdentity();
}
