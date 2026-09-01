// <copyright file="AccountKeyDerivation.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
    public static string HkdfSalt => "CipherBank";

    public static string HkdfInfo => "account/x25519/v1";

    /// <summary>Convenience for A1 suite id checks.</summary>
    public static string DefaultAlgorithmId => X25519ChaChaSealAlgorithm.AlgorithmIdValue;

    public static AccountKeyPair DeriveAccountKey(ISealAlgorithm algorithm, ReadOnlySpan<byte> bip39Entropy)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        if (bip39Entropy.IsEmpty)
        {
            throw new ArgumentException("Entropy required.", nameof(bip39Entropy));
        }

        byte[] seed = new byte[32];
        byte[] saltBytes = Encoding.UTF8.GetBytes(HkdfSalt);
        byte[] infoBytes = Encoding.UTF8.GetBytes(HkdfInfo);
        try
        {
            HKDF.DeriveKey(HashAlgorithmName.SHA256, bip39Entropy, seed, saltBytes, infoBytes);
            return algorithm.DeriveKeyPair(seed);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed);
            CryptographicOperations.ZeroMemory(saltBytes);
            CryptographicOperations.ZeroMemory(infoBytes);
        }
    }
}
