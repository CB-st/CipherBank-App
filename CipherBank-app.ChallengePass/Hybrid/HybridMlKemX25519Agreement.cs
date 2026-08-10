// <copyright file="HybridMlKemX25519Agreement.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Crypto;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>
/// Hybrid key share: ML-KEM-768 + X25519 → 32-byte PQ channel key via HKDF.
/// </summary>
public sealed class HybridMlKemX25519Agreement
{
    private const int IdentitySeedBytes = 96;
    private const int X25519SeedBytes = 32;
    private const int MlKemSeedBytes = 64;
    private const int ChannelKeyBytes = 32;
    private const int KeyShareIdHexLength = 16;

    private static readonly byte[] HkdfSalt = Encoding.UTF8.GetBytes("CipherBank-pq-channel");
    private static readonly byte[] HkdfInfo = Encoding.UTF8.GetBytes("pq-channel/v1");
    private static readonly byte[] IdentitySalt = Encoding.UTF8.GetBytes("CipherBank");
    private static readonly byte[] IdentityInfo = Encoding.UTF8.GetBytes("account/hybrid/v1");

    private readonly X25519ChaChaSealAlgorithm _x25519 = new();

    public static string KeyShareAlgorithmId => "hybrid-mlkem768-x25519-v1";

    public static string ChannelAlgorithmId => "pq-channel-chacha20poly1305-v1";

    /// <summary>
    /// Server creates a key-share for the device public identity; returns response + channel key.
    /// KEM shared secret is always zeroed in finally even when later ECDH / HKDF steps fail.
    /// Use: High (every A2 key-share). Scope: hybrid agreement server path.
    /// </summary>
    public static (PqKeyShareResponse Response, byte[] ChannelKey) CreateShareAsServer(HybridPublicIdentity device)
    {
        var deviceMlKemPk = WireEncoding.FromWire(device.MlKemPublicKeyWire);
        var deviceX25519Pk = WireEncoding.FromWire(device.X25519PublicKeyWire);

        (var kemCt, var ssKem) = MlKem768Provider.Encapsulate(deviceMlKemPk);
        byte[]? ephSk = null;
        byte[]? ssX = null;
        try
        {
            (var ephPk, ephSk) = PortableX25519.GenerateKeyPair();
            ssX = PortableX25519.Agree(ephSk, deviceX25519Pk);
            var channelKey = Combine(ssKem, ssX);

            var response = new PqKeyShareResponse
            {
                KeyShareId = "ks_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..KeyShareIdHexLength],
                MlKemCiphertextWire = WireEncoding.ToWire(kemCt),
                ServerX25519PublicKeyWire = WireEncoding.ToWire(ephPk),
                Algorithm = KeyShareAlgorithmId,
            };
            return (response, channelKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ssKem);
            if (ssX is not null)
            {
                CryptographicOperations.ZeroMemory(ssX);
            }

            if (ephSk is not null)
            {
                CryptographicOperations.ZeroMemory(ephSk);
            }
        }
    }

    /// <summary>
    /// Device completes key-share → channel key.
    /// KEM shared secret is zeroed in finally, including when server key-wire decode or X25519 agreement fails.
    /// Use: High (every A2 key-share). Scope: hybrid agreement device path.
    /// </summary>
    public static byte[] CompleteAsDevice(HybridPrivateIdentity identity, PqKeyShareResponse server)
    {
        var kemCt = WireEncoding.FromWire(server.MlKemCiphertextWire);
        var ssKem = MlKem768Provider.Decapsulate(kemCt, identity.MlKemPrivateKey);
        byte[]? ssX = null;
        try
        {
            var serverPkBytes = WireEncoding.FromWire(server.ServerX25519PublicKeyWire);
            ssX = PortableX25519.Agree(identity.X25519PrivateKey, serverPkBytes);
            return Combine(ssKem, ssX);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ssKem);
            if (ssX is not null)
            {
                CryptographicOperations.ZeroMemory(ssX);
            }
        }
    }

    /// <summary>Derive hybrid device identity from BIP39 entropy (or any IKM).</summary>
    public HybridPrivateIdentity DeriveIdentity(ReadOnlySpan<byte> bip39Entropy)
    {
        var seed96 = new byte[IdentitySeedBytes];
        try
        {
            HKDF.DeriveKey(HashAlgorithmName.SHA256, bip39Entropy, seed96, IdentitySalt, IdentityInfo);
            AccountKeyPair x = _x25519.DeriveKeyPair(seed96.AsSpan(0, X25519SeedBytes));
            (var mlPub, var mlPriv) = MlKem768Provider.GenerateKeyPairFromSeed(
                seed96.AsSpan(X25519SeedBytes, MlKemSeedBytes));
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

    /// <summary>
    /// HKDF-combines KEM and X25519 shared secrets into the 32-byte channel key.
    /// Use: Medium (each key-share). Scope: hybrid agreement.
    /// </summary>
    private static byte[] Combine(ReadOnlySpan<byte> ssKem, ReadOnlySpan<byte> ssX)
    {
        var ikm = new byte[ssKem.Length + ssX.Length];
        ssKem.CopyTo(ikm);
        ssX.CopyTo(ikm.AsSpan(ssKem.Length));
        try
        {
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, ChannelKeyBytes, HkdfSalt, HkdfInfo);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ikm);
        }
    }
}
