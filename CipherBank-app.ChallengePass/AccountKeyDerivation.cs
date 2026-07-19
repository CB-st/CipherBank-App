// <copyright file="AccountKeyDerivation.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using CipherBank_app.ChallengePass.Algorithms;

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Derives account X25519 seed from BIP39 entropy via HKDF (spec A1).
/// Kept separate from seal slot so key derivation can version independently.
/// </summary>
public static class AccountKeyDerivation
{
    public const string HkdfSalt = "CipherBank";
    public const string HkdfInfo = "account/x25519/v1";

    public static AccountKeyPair DeriveAccountKey(ISealAlgorithm algorithm, ReadOnlySpan<byte> bip39Entropy)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        if (bip39Entropy.IsEmpty)
        {
            throw new ArgumentException("Entropy required.", nameof(bip39Entropy));
        }

        byte[] seed = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            bip39Entropy.ToArray(),
            32,
            Encoding.UTF8.GetBytes(HkdfSalt),
            Encoding.UTF8.GetBytes(HkdfInfo));
        try
        {
            return algorithm.DeriveKeyPair(seed);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed);
        }
    }

    /// <summary>Convenience for A1 suite id checks.</summary>
    public static string DefaultAlgorithmId => X25519ChaChaSealAlgorithm.AlgorithmIdValue;
}
