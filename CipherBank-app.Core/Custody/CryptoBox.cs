// <copyright file="CryptoBox.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace CipherBank_app.Custody;

/// <summary>AES-GCM seal/open for custody blobs (Cora cryptoBox parity).</summary>
public static class CryptoBox
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int SaltSize = 16;
    private const int Iterations = 210_000;

    public static byte[] DeriveKey(string pin, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);
    }

    public static string Seal(string plaintext, string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = DeriveKey(pin, salt);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];
        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plain, cipher, tag);
        }

        var packed = new byte[SaltSize + NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(salt, 0, packed, 0, SaltSize);
        Buffer.BlockCopy(nonce, 0, packed, SaltSize, NonceSize);
        Buffer.BlockCopy(tag, 0, packed, SaltSize + NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, packed, SaltSize + NonceSize + TagSize, cipher.Length);
        CryptographicOperations.ZeroMemory(key);
        CryptographicOperations.ZeroMemory(plain);
        return Convert.ToBase64String(packed);
    }

    public static string Open(string sealedB64, string pin)
    {
        var packed = Convert.FromBase64String(sealedB64);
        if (packed.Length < SaltSize + NonceSize + TagSize + 1)
        {
            throw new CryptographicException("Invalid sealed blob.");
        }

        var salt = packed.AsSpan(0, SaltSize).ToArray();
        var nonce = packed.AsSpan(SaltSize, NonceSize).ToArray();
        var tag = packed.AsSpan(SaltSize + NonceSize, TagSize).ToArray();
        var cipher = packed.AsSpan(SaltSize + NonceSize + TagSize).ToArray();
        var key = DeriveKey(pin, salt);
        var plain = new byte[cipher.Length];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }
    }
}
