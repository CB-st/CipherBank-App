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
    /// Versioned seals open on the 0x01 layout and a flipped ciphertext bit is rejected.
    /// Use: High (custody blob). Scope: AesGcmCryptoBox.Open.
    /// </summary>
    [Fact]
    public void Open_RoundTripsVersionedLayout_AndRejectsTamper()
    {
        AesGcmCryptoBox box = new AesGcmCryptoBox(CryptographyOptions.Default);
        const string pin = "123456";
        const string plaintext = "custody-secret";

        string versioned = box.Seal(plaintext, pin);
        byte[] versionedBytes = Convert.FromBase64String(versioned);
        versionedBytes[0].Should().Be(0x01);
        box.Open(versioned, pin).Should().Be(plaintext);

        versionedBytes[^1] ^= 0x01;
        Action tampered = () => box.Open(Convert.ToBase64String(versionedBytes), pin);
        tampered.Should().Throw<CryptographicException>();
    }

    /// <summary>
    /// A blob without the version byte is rejected even when its own tag would authenticate,
    /// including when the first salt byte happens to equal the version marker.
    /// Use: High (custody blob). Scope: AesGcmCryptoBox.Open.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData((byte)0x01)]
    public void Open_RejectsUnversionedLayout(byte? saltPrefix)
    {
        CryptographyOptions options = CryptographyOptions.Default;
        AesGcmCryptoBox box = new AesGcmCryptoBox(options);
        Action act = () => box.Open(UnversionedBlob(box, options, "123456", "custody-secret", saltPrefix), "123456");
        act.Should().Throw<CryptographicException>();
    }

    /// <summary>
    /// A wrong version byte or a blob too short to hold the header is rejected.
    /// Use: High (custody blob). Scope: AesGcmCryptoBox.Open.
    /// </summary>
    [Fact]
    public void Open_RejectsWrongVersionAndShortBlob()
    {
        AesGcmCryptoBox box = new AesGcmCryptoBox(CryptographyOptions.Default);
        byte[] bytes = Convert.FromBase64String(box.Seal("custody-secret", "123456"));
        bytes[0] = 0x02;
        Action wrongVersion = () => box.Open(Convert.ToBase64String(bytes), "123456");
        wrongVersion.Should().Throw<CryptographicException>();

        Action tooShort = () => box.Open(Convert.ToBase64String(new byte[] { 0x01, 0x00 }), "123456");
        tooShort.Should().Throw<CryptographicException>();
    }

    private static string UnversionedBlob(
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

        using SensitiveBuffer keyBuffer = box.DeriveKey(pin, salt);
        byte[] key = keyBuffer.Span.ToArray();
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
