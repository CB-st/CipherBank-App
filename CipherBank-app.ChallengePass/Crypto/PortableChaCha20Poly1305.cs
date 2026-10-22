// <copyright file="PortableChaCha20Poly1305.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.ChallengePass.Crypto;

/// <summary>Portable ChaCha20-Poly1305 via BCL (no native dependency).</summary>
internal static class PortableChaCha20Poly1305
{
    internal const int KeySize = 32;
    internal const int NonceSize = 12;
    internal const int TagSize = 16;

    internal static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext)
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
        using ChaCha20Poly1305 aead = new ChaCha20Poly1305(key);
        aead.Encrypt(nonce, plaintext, ciphertext, tag);
        byte[] result = new byte[ciphertext.Length + TagSize];
        ciphertext.CopyTo(result.AsSpan(0, ciphertext.Length));
        tag.CopyTo(result.AsSpan(ciphertext.Length));
        return result;
    }

    internal static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertextAndTag)
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
        using ChaCha20Poly1305 aead = new ChaCha20Poly1305(key);
        aead.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
