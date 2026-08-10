// <copyright file="AesGcmCryptoBox.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using CipherBank_app.Configuration;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Custody;

/// <summary>AES-GCM custody box with configuration-backed PBKDF2 parameters.</summary>
public sealed class AesGcmCryptoBox : ICryptoBox
{
    private readonly CryptographyOptions _options;

    public AesGcmCryptoBox(IOptions<CryptographyOptions> options)
        : this(options.Value)
    {
    }

    public AesGcmCryptoBox(CryptographyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsValid())
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Cryptography parameters are unsafe or incompatible.");
        }

        _options = options;
    }

    public byte[] DeriveKey(string pin, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin),
            salt,
            _options.Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            _options.KeySizeBytes);

    public string Seal(string plaintext, string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        var key = DeriveKey(pin, salt);
        var nonce = RandomNumberGenerator.GetBytes(_options.NonceSizeBytes);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[_options.TagSizeBytes];
        try
        {
            using var aes = new AesGcm(key, _options.TagSizeBytes);
            aes.Encrypt(nonce, plain, cipher, tag);

            var packed = new byte[salt.Length + nonce.Length + tag.Length + cipher.Length];
            var offset = 0;
            Buffer.BlockCopy(salt, 0, packed, offset, salt.Length);
            offset += salt.Length;
            Buffer.BlockCopy(nonce, 0, packed, offset, nonce.Length);
            offset += nonce.Length;
            Buffer.BlockCopy(tag, 0, packed, offset, tag.Length);
            offset += tag.Length;
            Buffer.BlockCopy(cipher, 0, packed, offset, cipher.Length);
            return Convert.ToBase64String(packed);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    public string Open(string sealedB64, string pin)
    {
        var packed = Convert.FromBase64String(sealedB64);
        var headerSize = _options.SaltSizeBytes + _options.NonceSizeBytes + _options.TagSizeBytes;
        if (packed.Length <= headerSize)
        {
            throw new CryptographicException("Invalid sealed blob.");
        }

        var offset = 0;
        var salt = packed.AsSpan(offset, _options.SaltSizeBytes).ToArray();
        offset += salt.Length;
        var nonce = packed.AsSpan(offset, _options.NonceSizeBytes).ToArray();
        offset += nonce.Length;
        var tag = packed.AsSpan(offset, _options.TagSizeBytes).ToArray();
        offset += tag.Length;
        var cipher = packed.AsSpan(offset).ToArray();
        var key = DeriveKey(pin, salt);
        var plain = new byte[cipher.Length];
        try
        {
            using var aes = new AesGcm(key, _options.TagSizeBytes);
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
