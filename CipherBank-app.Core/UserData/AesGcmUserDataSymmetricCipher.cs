// <copyright file="AesGcmUserDataSymmetricCipher.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.UserData;

/// <summary>AES-256-GCM symmetric primitive for pack blocks and Core-internal wrapping.</summary>
public sealed class AesGcmUserDataSymmetricCipher : IUserDataSymmetricCipher
{
    public string AlgorithmId => UserDataConstants.SymmetricAlgorithmAesGcmV1;

    /// <inheritdoc />
    public UserDataSymmetricBlob Seal(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key32,
        ReadOnlySpan<byte> aad)
    {
        EnsureKey(key32);
        byte[] nonce = RandomNumberGenerator.GetBytes(UserDataConstants.NonceSize);
        return SealWithNonce(plaintext, key32, aad, nonce);
    }

    /// <inheritdoc />
    public UserDataSymmetricBlob SealWithNonce(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key32,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> nonce12)
    {
        EnsureKey(key32);
        if (nonce12.Length != UserDataConstants.NonceSize)
        {
            throw new ArgumentException($"Nonce must be {UserDataConstants.NonceSize} bytes.", nameof(nonce12));
        }

        byte[] nonce = nonce12.ToArray();
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[UserDataConstants.TagSize];
        byte[] plainCopy = plaintext.ToArray();
        byte[] aadCopy = aad.Length == 0 ? [] : aad.ToArray();

        try
        {
            using AesGcm aes = new AesGcm(key32, UserDataConstants.TagSize);
            aes.Encrypt(nonce, plainCopy, ciphertext, tag, aad.Length == 0 ? null : aadCopy);
            return new UserDataSymmetricBlob(nonce, tag, ciphertext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainCopy);
            if (aadCopy.Length > 0)
            {
                CryptographicOperations.ZeroMemory(aadCopy);
            }
        }
    }

    /// <inheritdoc />
    public byte[] Open(
        UserDataSymmetricBlob blob,
        ReadOnlySpan<byte> key32,
        ReadOnlySpan<byte> aad)
    {
        ArgumentNullException.ThrowIfNull(blob);
        EnsureKey(key32);
        if (blob.Nonce.Length != UserDataConstants.NonceSize ||
            blob.Tag.Length != UserDataConstants.TagSize)
        {
            throw new CryptographicException("Invalid symmetric blob sizes.");
        }

        byte[] plaintext = new byte[blob.Ciphertext.Length];
        byte[] aadCopy = aad.Length == 0 ? [] : aad.ToArray();
        try
        {
            using AesGcm aes = new AesGcm(key32, UserDataConstants.TagSize);
            aes.Decrypt(
                blob.Nonce,
                blob.Ciphertext,
                blob.Tag,
                plaintext,
                aad.Length == 0 ? null : aadCopy);
            return plaintext;
        }
        finally
        {
            if (aadCopy.Length > 0)
            {
                CryptographicOperations.ZeroMemory(aadCopy);
            }
        }
    }

    private static void EnsureKey(ReadOnlySpan<byte> key32)
    {
        if (key32.Length != UserDataConstants.KekLength)
        {
            throw new ArgumentException($"Key must be {UserDataConstants.KekLength} bytes.", nameof(key32));
        }
    }
}
