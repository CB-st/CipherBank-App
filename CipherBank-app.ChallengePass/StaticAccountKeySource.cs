// <copyright file="StaticAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.ChallengePass.Hybrid;

namespace CipherBank_app.ChallengePass;

/// <summary>Test/lab account key source with a fixed in-memory keypair / hybrid identity.</summary>
public sealed class StaticAccountKeySource(AccountKeyPair pair, HybridPrivateIdentity? hybrid) : IAccountKeySource, IDisposable
{
    private readonly AccountKeyPair _pair = new(CopyBuffer(pair.PublicKey), CopyBuffer(pair.PrivateKey));
    private readonly HybridPrivateIdentity? _hybrid = CopyHybrid(hybrid);
    private bool _disposed;

    public StaticAccountKeySource(AccountKeyPair pair)
        : this(pair, null)
    {
    }

    /// <summary>
    /// Returns a fresh AccountKeyPair whose buffers are copies of the fixture keys.
    /// Use: High (lab A1 proofs). Scope: StaticAccountKeySource retained pair.
    /// </summary>
    public AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = algorithm;

        // Hand out copies so callers (e.g. ChallengePassSessionProofBuilder) can ZeroMemory
        // their buffers without destroying the lab fixture's retained key material.
        return new AccountKeyPair([.. _pair.PublicKey], [.. _pair.PrivateKey]);
    }

    /// <summary>
    /// Returns a new HybridPrivateIdentity with all four buffers copied from the fixture.
    /// Use: High (lab A2 proofs). Scope: StaticAccountKeySource retained hybrid.
    /// </summary>
    public HybridPrivateIdentity RequireHybridIdentity()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_hybrid is null)
        {
            throw new InvalidOperationException("No hybrid identity configured on StaticAccountKeySource.");
        }

        // Same copy discipline as RequireUnlockedKeyPair: ClearDeviceIdentity / Dispose on the
        // PQ structure zeroes the adopted buffers and must not brick the fixture for a second build.
        return new HybridPrivateIdentity
        {
            X25519PublicKey = [.. _hybrid.X25519PublicKey],
            X25519PrivateKey = [.. _hybrid.X25519PrivateKey],
            MlKemPublicKey = [.. _hybrid.MlKemPublicKey],
            MlKemPrivateKey = [.. _hybrid.MlKemPrivateKey],
        };
    }

    /// <summary>
    /// Zeroes retained private key and hybrid private buffers when the fixture leaves scope.
    /// Use: Low (dispose). Scope: StaticAccountKeySource retained material.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_pair.PrivateKey);
        if (_hybrid is not null)
        {
            CryptographicOperations.ZeroMemory(_hybrid.X25519PrivateKey);
            CryptographicOperations.ZeroMemory(_hybrid.MlKemPrivateKey);
        }

        _disposed = true;
    }

    private static HybridPrivateIdentity? CopyHybrid(HybridPrivateIdentity? hybrid)
    {
        if (hybrid is null)
        {
            return null;
        }

        return new HybridPrivateIdentity
        {
            X25519PublicKey = CopyBuffer(hybrid.X25519PublicKey),
            X25519PrivateKey = CopyBuffer(hybrid.X25519PrivateKey),
            MlKemPublicKey = CopyBuffer(hybrid.MlKemPublicKey),
            MlKemPrivateKey = CopyBuffer(hybrid.MlKemPrivateKey),
        };
    }

    private static byte[] CopyBuffer(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return [.. source];
    }
}
