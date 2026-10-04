// <copyright file="AesGcmCryptoBox.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
    /// Packed-blob layout marker. This is a wire constant, not a <see cref="CryptographyOptions"/>
    /// setting: the persisted profile is frozen, and a config-controlled version would let an
    /// appsettings edit change the on-disk envelope.
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

            // v1: [version][salt][nonce][tag][cipher]. Bytes, not a StringBuilder:
            // the buffer holds key-derived ciphertext and must be able to be zeroed.
            byte[] packed = new byte[1 + salt.Length + nonce.Length + tag.Length + cipher.Length];
            Span<byte> destination = packed;
            destination[0] = BlobFormatVersion;
            destination = destination[1..];
            salt.CopyTo(destination);
            destination = destination[salt.Length..];
            nonce.CopyTo(destination);
            destination = destination[nonce.Length..];
            tag.CopyTo(destination);
            destination = destination[tag.Length..];
            cipher.CopyTo(destination);
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

        // Prefer legacy layout first: a random salt may begin with 0x01 (~1/256), so treating that
        // as a version marker would mis-slice fields. Versioned blobs still decrypt on fallback.
        if (TryOpenAt(packed, dataOffset: 0, pin, out string? legacy) && legacy is not null)
        {
            return legacy;
        }

        if (packed.Length > 0
            && packed[0] == BlobFormatVersion
            && TryOpenAt(packed, dataOffset: 1, pin, out string? versioned)
            && versioned is not null)
        {
            return versioned;
        }

        throw new CryptographicException("Invalid sealed blob.");
    }

    private bool TryOpenAt(byte[] packed, int dataOffset, string pin, out string? plaintext)
    {
        plaintext = null;
        int headerSize = _options.SaltSizeBytes + _options.NonceSizeBytes + _options.TagSizeBytes;
        if (packed.Length <= dataOffset + headerSize)
        {
            return false;
        }

        int offset = dataOffset;
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
            plaintext = Encoding.UTF8.GetString(plain);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }
    }
}
