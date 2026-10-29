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
    private readonly CryptographyOptions _options;

    public AesGcmCryptoBox(IOptions<CryptographyOptions> options)
        : this(options.Value)
    {
    }

    public AesGcmCryptoBox(CryptographyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Keep a private copy: the caller's instance is mutable and must not change this box later.
        _options = new CryptographyOptions
        {
            NonceSizeBytes = options.NonceSizeBytes,
            TagSizeBytes = options.TagSizeBytes,
            KeySizeBytes = options.KeySizeBytes,
            SaltSizeBytes = options.SaltSizeBytes,
            Pbkdf2Iterations = options.Pbkdf2Iterations,
            BlobFormatVersion = options.BlobFormatVersion,
        };

        // Freeze the profile at the box boundary, not only in DI: a box built from any other
        // profile could seal a blob the host cannot open.
        if (!_options.MatchesPersistedProfile())
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Cryptography parameters do not match the persisted custody profile.");
        }
    }

    public SensitiveBuffer DeriveKey(string pin, byte[] salt)
    {
        ArgumentNullException.ThrowIfNull(pin);
        using SensitiveBuffer password = new(Encoding.UTF8.GetBytes(pin));
        byte[] key = new byte[_options.KeySizeBytes];
        Rfc2898DeriveBytes.Pbkdf2(password.Span, salt, key, _options.Pbkdf2Iterations, HashAlgorithmName.SHA256);
        return new SensitiveBuffer(key);
    }

    public string Seal(string plaintext, string pin)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        using SensitiveBuffer key = DeriveKey(pin, salt);
        using SensitiveBuffer plain = new(Encoding.UTF8.GetBytes(plaintext));
        byte[] nonce = RandomNumberGenerator.GetBytes(_options.NonceSizeBytes);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[_options.TagSizeBytes];
        using AesGcm aes = new AesGcm(key.Span, _options.TagSizeBytes);
        aes.Encrypt(nonce, plain.Span, cipher, tag);

        // v1: [version][salt][nonce][tag][cipher]. Bytes, not a StringBuilder:
        // the buffer holds key-derived ciphertext and must be able to be zeroed.
        byte[] packed = new byte[1 + salt.Length + nonce.Length + tag.Length + cipher.Length];
        Span<byte> destination = packed;
        destination[0] = _options.BlobFormatVersion;
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

    public string Open(string sealedB64, string pin)
    {
        byte[] packed = Convert.FromBase64String(sealedB64);

        // Every Seal blob starts with 0x01. Try that layout first so a normal unlock pays one KDF.
        // A legacy salt can also start with 0x01. If the versioned tag does not authenticate, the
        // legacy layout is the other candidate; it is accepted only when its own tag authenticates.
        // The marker is a public format byte. Tag comparison stays inside AesGcm.
        if (packed.Length > 0
            && packed[0] == _options.BlobFormatVersion
            && TryOpenAt(packed, dataOffset: 1, pin, out string? versioned)
            && versioned is not null)
        {
            return versioned;
        }

        if (TryOpenAt(packed, dataOffset: 0, pin, out string? legacy) && legacy is not null)
        {
            return legacy;
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
        using SensitiveBuffer cipher = new(packed.AsSpan(offset).ToArray());
        using SensitiveBuffer key = DeriveKey(pin, salt);
        using SensitiveBuffer plain = new(new byte[cipher.Length]);
        try
        {
            using AesGcm aes = new AesGcm(key.Span, _options.TagSizeBytes);
            aes.Decrypt(nonce, cipher.Span, tag, plain.Span);
            plaintext = Encoding.UTF8.GetString(plain.Span);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
