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
    /// <summary>
    /// Current packed-blob version. Follow-up: encode salt/tag/key/iteration sizes in-band when
    /// CryptographyOptions may diverge across releases.
    /// </summary>
    private const byte BlobFormatVersion = 0x01;

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
        byte[] salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        byte[] key = DeriveKey(pin, salt);
        byte[] nonce = RandomNumberGenerator.GetBytes(_options.NonceSizeBytes);
        byte[] plain = Encoding.UTF8.GetBytes(plaintext);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[_options.TagSizeBytes];
        try
        {
            using AesGcm aes = new AesGcm(key, _options.TagSizeBytes);
            aes.Encrypt(nonce, plain, cipher, tag);

            // v1: [version][salt][nonce][tag][cipher] — version documents the packing layout.
            byte[] packed = new byte[1 + salt.Length + nonce.Length + tag.Length + cipher.Length];
            int offset = 0;
            packed[offset++] = BlobFormatVersion;
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
        byte[] packed = Convert.FromBase64String(sealedB64);
        int offset = 0;
        if (packed.Length > 0 && packed[0] == BlobFormatVersion)
        {
            offset = 1;
        }

        // Legacy blobs omit the version byte; both layouts use current CryptographyOptions sizes.
        int headerSize = _options.SaltSizeBytes + _options.NonceSizeBytes + _options.TagSizeBytes;
        if (packed.Length <= offset + headerSize)
        {
            throw new CryptographicException("Invalid sealed blob.");
        }

        byte[] salt = packed.AsSpan(offset, _options.SaltSizeBytes).ToArray();
        offset += salt.Length;
        byte[] nonce = packed.AsSpan(offset, _options.NonceSizeBytes).ToArray();
        offset += nonce.Length;
        byte[] tag = packed.AsSpan(offset, _options.TagSizeBytes).ToArray();
        offset += tag.Length;
        byte[] cipher = packed.AsSpan(offset).ToArray();
        byte[] key = DeriveKey(pin, salt);
        byte[] plain = new byte[cipher.Length];
        try
        {
            using AesGcm aes = new AesGcm(key, _options.TagSizeBytes);
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
