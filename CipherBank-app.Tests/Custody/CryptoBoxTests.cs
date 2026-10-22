// <copyright file="CryptoBoxTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class CryptoBoxTests
{
    [Fact]
    public void SealOpen_RoundTripsPlaintext()
    {
        string sealedBlob = CryptoBox.Seal(CryptographyOptions.Default, "alpha beta gamma", "123456");
        CryptoBox.Open(CryptographyOptions.Default, sealedBlob, "123456").Should().Be("alpha beta gamma");
    }

    [Fact]
    public void Open_WithWrongPin_Throws()
    {
        string sealedBlob = CryptoBox.Seal(CryptographyOptions.Default, "secret", "123456");
        Action act = () => CryptoBox.Open(CryptographyOptions.Default, sealedBlob, "000000");
        act.Should().Throw<Exception>();
    }

    /// <summary>
    /// Versioned seals open on the 0x01 layout, legacy seals still open, and a flipped ciphertext bit is rejected.
    /// Use: High (custody blob). Scope: AesGcmCryptoBox.Open.
    /// </summary>
    [Fact]
    public void Open_RoundTripsVersionedAndLegacyLayouts_AndRejectsTamper()
    {
        CryptographyOptions options = CryptographyOptions.Default;
        AesGcmCryptoBox box = new AesGcmCryptoBox(options);
        const string pin = "123456";
        const string plaintext = "custody-secret";

        string versioned = box.Seal(plaintext, pin);
        byte[] versionedBytes = Convert.FromBase64String(versioned);
        versionedBytes[0].Should().Be(0x01);
        box.Open(versioned, pin).Should().Be(plaintext);

        box.Open(LegacyBlob(box, options, pin, plaintext, saltPrefix: null), pin).Should().Be(plaintext);
        box.Open(LegacyBlob(box, options, pin, plaintext, saltPrefix: 0x01), pin).Should().Be(plaintext);

        versionedBytes[^1] ^= 0x01;
        Action tampered = () => box.Open(Convert.ToBase64String(versionedBytes), pin);
        tampered.Should().Throw<CryptographicException>();
    }

    private static string LegacyBlob(
        AesGcmCryptoBox box,
        CryptographyOptions options,
        string pin,
        string plaintext,
        byte? saltPrefix)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(options.SaltSizeBytes);
        if (saltPrefix is byte prefix)
        {
            salt[0] = prefix;
        }

        byte[] key = box.DeriveKey(pin, salt);
        byte[] nonce = RandomNumberGenerator.GetBytes(options.NonceSizeBytes);
        byte[] plain = Encoding.UTF8.GetBytes(plaintext);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[options.TagSizeBytes];
        try
        {
            using AesGcm aes = new AesGcm(key, options.TagSizeBytes);
            aes.Encrypt(nonce, plain, cipher, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }

        byte[] packed = new byte[salt.Length + nonce.Length + tag.Length + cipher.Length];
        salt.CopyTo(packed.AsSpan(0));
        nonce.CopyTo(packed.AsSpan(salt.Length));
        tag.CopyTo(packed.AsSpan(salt.Length + nonce.Length));
        cipher.CopyTo(packed.AsSpan(salt.Length + nonce.Length + tag.Length));
        return Convert.ToBase64String(packed);
    }
}
