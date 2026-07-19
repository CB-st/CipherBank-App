// <copyright file="HybridMlKemX25519Agreement.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using CipherBank_app.ChallengePass.Algorithms;
using NSec.Cryptography;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>
/// Hybrid key share: ML-KEM-768 + X25519 → 32-byte PQ channel key via HKDF.
/// </summary>
public sealed class HybridMlKemX25519Agreement
{
    public const string KeyShareAlgorithmId = "hybrid-mlkem768-x25519-v1";
    public const string ChannelAlgorithmId = "pq-channel-chacha20poly1305-v1";

    private static readonly byte[] HkdfSalt = Encoding.UTF8.GetBytes("CipherBank-pq-channel");
    private static readonly byte[] HkdfInfo = Encoding.UTF8.GetBytes("pq-channel/v1");
    private static readonly byte[] IdentitySalt = Encoding.UTF8.GetBytes("CipherBank");
    private static readonly byte[] IdentityInfo = Encoding.UTF8.GetBytes("account/hybrid/v1");

    private readonly X25519ChaChaSealAlgorithm _x25519 = new();

    /// <summary>Derive hybrid device identity from BIP39 entropy (or any IKM).</summary>
    public HybridPrivateIdentity DeriveIdentity(ReadOnlySpan<byte> bip39Entropy)
    {
        byte[] seed96 = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            bip39Entropy.ToArray(),
            96,
            IdentitySalt,
            IdentityInfo);
        try
        {
            AccountKeyPair x = _x25519.DeriveKeyPair(seed96.AsSpan(0, 32));
            (byte[] mlPub, byte[] mlPriv) = MlKem768Provider.GenerateKeyPairFromSeed(seed96.AsSpan(32, 64));
            return new HybridPrivateIdentity
            {
                X25519PublicKey = x.PublicKey,
                X25519PrivateKey = x.PrivateKey,
                MlKemPublicKey = mlPub,
                MlKemPrivateKey = mlPriv,
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed96);
        }
    }

    /// <summary>Server creates a key-share for the device public identity; returns response + channel key.</summary>
    public (PqKeyShareResponse Response, byte[] ChannelKey) CreateShareAsServer(HybridPublicIdentity device)
    {
        byte[] deviceMlKemPk = WireEncoding.FromWire(device.MlKemPublicKeyWire);
        byte[] deviceX25519Pk = WireEncoding.FromWire(device.X25519PublicKeyWire);

        (byte[] kemCt, byte[] ssKem) = MlKem768Provider.Encapsulate(deviceMlKemPk);

        var creation = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using var eph = Key.Create(KeyAgreementAlgorithm.X25519, creation);
        byte[] ephPk = eph.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        PublicKey devicePk = PublicKey.Import(KeyAgreementAlgorithm.X25519, deviceX25519Pk, KeyBlobFormat.RawPublicKey);
        var sharedParams = new SharedSecretCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using SharedSecret shared = KeyAgreementAlgorithm.X25519.Agree(eph, devicePk, ref sharedParams)
            ?? throw new CryptographicException("X25519 agree failed.");
        byte[] ssX = shared.Export(SharedSecretBlobFormat.RawSharedSecret);

        byte[] channelKey = Combine(ssKem, ssX);
        CryptographicOperations.ZeroMemory(ssKem);
        CryptographicOperations.ZeroMemory(ssX);

        var response = new PqKeyShareResponse
        {
            KeyShareId = "ks_" + Guid.NewGuid().ToString("N")[..16],
            MlKemCiphertextWire = WireEncoding.ToWire(kemCt),
            ServerX25519PublicKeyWire = WireEncoding.ToWire(ephPk),
            Algorithm = KeyShareAlgorithmId,
        };
        return (response, channelKey);
    }

    /// <summary>Device completes key-share → channel key.</summary>
    public byte[] CompleteAsDevice(HybridPrivateIdentity identity, PqKeyShareResponse server)
    {
        byte[] kemCt = WireEncoding.FromWire(server.MlKemCiphertextWire);
        byte[] ssKem = MlKem768Provider.Decapsulate(kemCt, identity.MlKemPrivateKey);

        byte[] serverPkBytes = WireEncoding.FromWire(server.ServerX25519PublicKeyWire);
        var creation = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using var deviceSk = Key.Import(
            KeyAgreementAlgorithm.X25519,
            identity.X25519PrivateKey,
            KeyBlobFormat.RawPrivateKey,
            creation);
        PublicKey serverPk = PublicKey.Import(
            KeyAgreementAlgorithm.X25519,
            serverPkBytes,
            KeyBlobFormat.RawPublicKey);
        var sharedParams = new SharedSecretCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using SharedSecret shared = KeyAgreementAlgorithm.X25519.Agree(deviceSk, serverPk, ref sharedParams)
            ?? throw new CryptographicException("X25519 agree failed.");
        byte[] ssX = shared.Export(SharedSecretBlobFormat.RawSharedSecret);

        try
        {
            return Combine(ssKem, ssX);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ssKem);
            CryptographicOperations.ZeroMemory(ssX);
        }
    }

    private static byte[] Combine(ReadOnlySpan<byte> ssKem, ReadOnlySpan<byte> ssX)
    {
        var ikm = new byte[ssKem.Length + ssX.Length];
        ssKem.CopyTo(ikm);
        ssX.CopyTo(ikm.AsSpan(ssKem.Length));
        try
        {
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, 32, HkdfSalt, HkdfInfo);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ikm);
        }
    }
}
