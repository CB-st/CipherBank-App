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
    public const int KeySize = 32;

    public static (byte[] PublicKey, byte[] PrivateKey) DeriveKeyPair(ReadOnlySpan<byte> seed32)
    {
        if (seed32.Length != KeySize)
        {
            throw new ArgumentException("Seed must be 32 bytes.", nameof(seed32));
        }

        var priv = new X25519PrivateKeyParameters(seed32);
        return (priv.GeneratePublicKey().GetEncoded(), priv.GetEncoded());
    }

    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
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

    public static byte[] Agree(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> peerPublicKey)
    {
        if (privateKey.Length != KeySize || peerPublicKey.Length != KeySize)
        {
            throw new ArgumentException("X25519 keys must be 32 bytes.");
        }

        var priv = new X25519PrivateKeyParameters(privateKey);
        var pub = new X25519PublicKeyParameters(peerPublicKey);
        var shared = new byte[KeySize];
        priv.GenerateSecret(pub, shared);
        return shared;
    }
}

/// <summary>Portable ChaCha20-Poly1305 via BCL (no native dependency).</summary>
internal static class PortableChaCha20Poly1305
{
    public const int KeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;

    public static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext)
    {
        if (key.Length != KeySize)
        {
            throw new ArgumentException("Key must be 32 bytes.", nameof(key));
        }

        if (nonce.Length != NonceSize)
        {
            throw new ArgumentException("Nonce must be 12 bytes.", nameof(nonce));
        }

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];
        using var aead = new ChaCha20Poly1305(key);
        aead.Encrypt(nonce, plaintext, ciphertext, tag);
        var result = new byte[ciphertext.Length + TagSize];
        ciphertext.CopyTo(result.AsSpan(0, ciphertext.Length));
        tag.CopyTo(result.AsSpan(ciphertext.Length));
        return result;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertextAndTag)
    {
        if (key.Length != KeySize)
        {
            throw new ArgumentException("Key must be 32 bytes.", nameof(key));
        }

        if (nonce.Length != NonceSize)
        {
            throw new ArgumentException("Nonce must be 12 bytes.", nameof(nonce));
        }

        if (ciphertextAndTag.Length < TagSize)
        {
            throw new CryptographicException("Ciphertext too short.");
        }

        int ctLen = ciphertextAndTag.Length - TagSize;
        ReadOnlySpan<byte> ciphertext = ciphertextAndTag[..ctLen];
        ReadOnlySpan<byte> tag = ciphertextAndTag[ctLen..];
        byte[] plaintext = new byte[ctLen];
        using var aead = new ChaCha20Poly1305(key);
        aead.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
