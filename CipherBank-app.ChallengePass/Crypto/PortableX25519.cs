// <copyright file="PortableX25519.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;

namespace CipherBank_app.ChallengePass.Crypto;

/// <summary>
/// Portable X25519 via BouncyCastle — avoids NSec/libsodium which ships a Linux .so
/// that fails to load on Android (missing libpthread.so.0).
/// </summary>
internal static class PortableX25519
{
    internal const int KeySize = 32;

    internal static (byte[] PublicKey, byte[] PrivateKey) DeriveKeyPair(ReadOnlySpan<byte> seed32)
    {
        if (seed32.Length != KeySize)
        {
            throw new ArgumentException("Seed must be 32 bytes.", nameof(seed32));
        }

        X25519PrivateKeyParameters priv = new X25519PrivateKeyParameters(seed32);
        return (priv.GeneratePublicKey().GetEncoded(), priv.GetEncoded());
    }

    internal static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
    {
        byte[] seed = RandomNumberGenerator.GetBytes(KeySize);
        try
        {
            return DeriveKeyPair(seed);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed);
        }
    }

    internal static byte[] Agree(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> peerPublicKey)
    {
        if (privateKey.Length != KeySize || peerPublicKey.Length != KeySize)
        {
            throw new ArgumentException("X25519 keys must be 32 bytes.");
        }

        X25519PrivateKeyParameters priv = new X25519PrivateKeyParameters(privateKey);
        X25519PublicKeyParameters pub = new X25519PublicKeyParameters(peerPublicKey);
        byte[] shared = new byte[KeySize];
        priv.GenerateSecret(pub, shared);
        return shared;
    }
}
