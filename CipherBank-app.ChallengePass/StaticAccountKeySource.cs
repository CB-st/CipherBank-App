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

    public StaticAccountKeySource(AccountKeyPair pair)
        : this(pair, null)
    {
    }

    public StaticAccountKeySource(AccountKeyPair pair, HybridPrivateIdentity? hybrid)
    {
        _pair = pair;
        _hybrid = hybrid;
    }

    /// <summary>
    /// Returns a fresh AccountKeyPair whose buffers are copies of the fixture keys.
    /// Use: High (lab A1 proofs). Scope: StaticAccountKeySource retained pair.
    /// </summary>
    public AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm)
    {
        _ = algorithm;

        // Hand out copies so callers (e.g. ChallengePassSessionProofBuilder) can ZeroMemory
        // their buffers without destroying the lab fixture's retained key material.
        return new AccountKeyPair(_pair.PublicKey.ToArray(), _pair.PrivateKey.ToArray());
    }

    /// <summary>
    /// Returns a new HybridPrivateIdentity with all four buffers copied from the fixture.
    /// Use: High (lab A2 proofs). Scope: StaticAccountKeySource retained hybrid.
    /// </summary>
    public HybridPrivateIdentity RequireHybridIdentity()
    {
        if (_hybrid is null)
        {
            throw new InvalidOperationException("No hybrid identity configured on StaticAccountKeySource.");
        }

        // Same copy discipline as RequireUnlockedKeyPair: ClearDeviceIdentity / Dispose on the
        // PQ structure zeroes the adopted buffers and must not brick the fixture for a second build.
        return new HybridPrivateIdentity
        {
            X25519PublicKey = _hybrid.X25519PublicKey.ToArray(),
            X25519PrivateKey = _hybrid.X25519PrivateKey.ToArray(),
            MlKemPublicKey = _hybrid.MlKemPublicKey.ToArray(),
            MlKemPrivateKey = _hybrid.MlKemPrivateKey.ToArray(),
        };
    }
}
