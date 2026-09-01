// <copyright file="MlKem768Provider.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Crypto.Parameters;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>
/// Portable ML-KEM-768 via BouncyCastle.Cryptography.
/// Uses <see cref="MLKemPrivateKeyParameters.FromSeed(MLKemParameters, byte[])"/> only — avoids SecureRandom
/// ambiguity with legacy BouncyCastle.Crypto pulled by NBitcoin.
/// </summary>
public static class MlKem768Provider
{
    public static readonly MLKemParameters Parameters = MLKemParameters.ml_kem_768;

    private const int SeedSizeBytes = 64;

    /// <summary>
    /// Deterministic keygen from 64-byte seed (ML-KEM-768 seed length).
    /// Temporary seed array for BouncyCastle is zeroed before return.
    /// Use: High (hybrid identity derive). Scope: MlKem768Provider.
    /// </summary>
    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromSeed(ReadOnlySpan<byte> seed64)
    {
        if (seed64.Length != SeedSizeBytes)
        {
            throw new ArgumentException("ML-KEM-768 seed must be 64 bytes.", nameof(seed64));
        }

        byte[] seedCopy = seed64.ToArray();
        try
        {
            MLKemPrivateKeyParameters priv = MLKemPrivateKeyParameters.FromSeed(Parameters, seedCopy);
            MLKemPublicKeyParameters pub = priv.GetPublicKey();
            return (pub.GetEncoded(), priv.GetEncoded());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seedCopy);
        }
    }

    /// <summary>
    /// Random keygen for tests / ephemeral use; zeroes the RNG seed after keygen returns.
    /// Use: Medium (tests / ephemeral). Scope: MlKem768Provider.
    /// </summary>
    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
    {
        byte[] seed = RandomNumberGenerator.GetBytes(SeedSizeBytes);
        try
        {
            return GenerateKeyPairFromSeed(seed);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed);
        }
    }

    public static (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(ReadOnlySpan<byte> recipientPublicKey)
    {
        byte[] pubCopy = recipientPublicKey.ToArray();
        try
        {
            MLKemPublicKeyParameters pub = MLKemPublicKeyParameters.FromEncoding(Parameters, pubCopy);
            MLKemEncapsulator enc = new(Parameters);
            enc.Init(pub);
            byte[] secret = new byte[enc.SecretLength];
            byte[] cipher = new byte[enc.EncapsulationLength];
            enc.Encapsulate(cipher, 0, cipher.Length, secret, 0, secret.Length);
            return (cipher, secret);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pubCopy);
        }
    }

    /// <summary>
    /// Decapsulates a ciphertext; temporary private-key / ciphertext arrays for BouncyCastle are zeroed.
    /// If Decapsulate fills then throws, the secret buffer is zeroed before rethrow.
    /// Use: High (A2 channel establish). Scope: MlKem768Provider.
    /// </summary>
    public static byte[] Decapsulate(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> recipientPrivateKey)
    {
        byte[] privCopy = recipientPrivateKey.ToArray();
        byte[] ctCopy = ciphertext.ToArray();
        byte[]? secret = null;
        try
        {
            MLKemPrivateKeyParameters priv = MLKemPrivateKeyParameters.FromEncoding(Parameters, privCopy);
            MLKemDecapsulator dec = new(Parameters);
            dec.Init(priv);
            secret = new byte[dec.SecretLength];
            dec.Decapsulate(ctCopy, 0, ctCopy.Length, secret, 0, secret.Length);
            byte[] owned = secret;
            secret = null;
            return owned;
        }
        finally
        {
            if (secret is not null)
            {
                CryptographicOperations.ZeroMemory(secret);
            }

            CryptographicOperations.ZeroMemory(privCopy);
            CryptographicOperations.ZeroMemory(ctCopy);
        }
    }
}
