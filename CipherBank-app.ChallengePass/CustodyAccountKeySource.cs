// <copyright file="CustodyAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.Custody;

namespace CipherBank_app.ChallengePass;

/// <summary>Derives A1 / A2 account keys from unlocked custody BIP39 entropy.</summary>
public sealed class CustodyAccountKeySource : IAccountKeySource
{
    private readonly ICustodyService _custody;
    private readonly HybridMlKemX25519Agreement _hybrid = new();

    public CustodyAccountKeySource(ICustodyService custody)
    {
        _custody = custody;
    }

    /// <summary>
    /// Derives the A1 account key pair from unlocked custody entropy; zeroes entropy after use.
    /// Use: High (every A1 proof). Scope: per-call entropy buffer.
    /// </summary>
    public AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm)
    {
        byte[] entropy = RequireEntropy();
        try
        {
            return AccountKeyDerivation.DeriveAccountKey(algorithm, entropy);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(entropy);
        }
    }

    /// <summary>
    /// Derives the A2 hybrid identity from unlocked custody entropy; zeroes entropy after use.
    /// Use: High (every A2 proof). Scope: per-call entropy buffer.
    /// </summary>
    public HybridPrivateIdentity RequireHybridIdentity()
    {
        byte[] entropy = RequireEntropy();
        try
        {
            return _hybrid.DeriveIdentity(entropy);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(entropy);
        }
    }

    /// <summary>
    /// Reads BIP39 entropy from unlocked custody.
    /// Use: Medium (called by key derivation). Scope: custody vault.
    /// </summary>
    private byte[] RequireEntropy()
    {
        string? mnemonic = _custody.ExportMnemonic();
        if (mnemonic is null)
        {
            throw new InvalidOperationException("Custody locked — unlock before challenge/pass.");
        }

        return MnemonicHelper.Entropy(mnemonic);
    }
}
