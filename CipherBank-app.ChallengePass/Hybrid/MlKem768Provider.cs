// <copyright file="MlKem768Provider.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Crypto.Parameters;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>
/// Portable ML-KEM-768 via BouncyCastle.Cryptography.
/// Uses <see cref="MLKemPrivateKeyParameters.FromSeed"/> only — avoids SecureRandom
/// ambiguity with legacy BouncyCastle.Crypto pulled by NBitcoin.
/// </summary>
public static class MlKem768Provider
{
    public static readonly MLKemParameters Parameters = MLKemParameters.ml_kem_768;

    /// <summary>Deterministic keygen from 64-byte seed (ML-KEM-768 seed length).</summary>
    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPairFromSeed(ReadOnlySpan<byte> seed64)
    {
        if (seed64.Length != 64)
        {
            throw new ArgumentException("ML-KEM-768 seed must be 64 bytes.", nameof(seed64));
        }

        var priv = MLKemPrivateKeyParameters.FromSeed(Parameters, seed64.ToArray());
        var pub = priv.GetPublicKey();
        return (pub.GetEncoded(), priv.GetEncoded());
    }

    /// <summary>Random keygen for tests / ephemeral use.</summary>
    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
        => GenerateKeyPairFromSeed(RandomNumberGenerator.GetBytes(64));


    public static (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(ReadOnlySpan<byte> recipientPublicKey)
    {
        var pub = MLKemPublicKeyParameters.FromEncoding(Parameters, recipientPublicKey.ToArray());
        var enc = new MLKemEncapsulator(Parameters);
        enc.Init(pub);
        byte[] secret = new byte[enc.SecretLength];
        byte[] cipher = new byte[enc.EncapsulationLength];
        enc.Encapsulate(cipher, 0, cipher.Length, secret, 0, secret.Length);
        return (cipher, secret);
    }

    public static byte[] Decapsulate(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> recipientPrivateKey)
    {
        var priv = MLKemPrivateKeyParameters.FromEncoding(Parameters, recipientPrivateKey.ToArray());
        var dec = new MLKemDecapsulator(Parameters);
        dec.Init(priv);
        byte[] secret = new byte[dec.SecretLength];
        dec.Decapsulate(ciphertext.ToArray(), 0, ciphertext.Length, secret, 0, secret.Length);
        return secret;
    }
}
