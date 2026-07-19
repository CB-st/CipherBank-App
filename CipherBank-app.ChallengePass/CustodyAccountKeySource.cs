// <copyright file="CustodyAccountKeySource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.Custody;

namespace CipherBank_app.ChallengePass;

/// <summary>Derives A1 / A2 account keys from unlocked custody BIP39 entropy.</summary>
public sealed class CustodyAccountKeySource : IAccountKeySource
{
    private readonly ICustodyService _custody;
    private readonly HybridMlKemX25519Agreement _hybrid = new();

    public CustodyAccountKeySource(ICustodyService custody) => _custody = custody;

    public AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm)
    {
        byte[] entropy = RequireEntropy();
        return AccountKeyDerivation.DeriveAccountKey(algorithm, entropy);
    }

    public HybridPrivateIdentity RequireHybridIdentity()
    {
        byte[] entropy = RequireEntropy();
        return _hybrid.DeriveIdentity(entropy);
    }

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
