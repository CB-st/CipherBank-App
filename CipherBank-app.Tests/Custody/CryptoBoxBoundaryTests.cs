// <copyright file="CryptoBoxBoundaryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

/// <summary>Profile freeze at the box boundary, envelope version, tamper, and buffer wiping.</summary>
public class CryptoBoxBoundaryTests
{
    private const string Pin = "123456";

    public static TheoryData<CryptographyOptions> DivergentProfiles => new()
    {
        CryptographyOptions.Default with { TagSizeBytes = 12 },
        CryptographyOptions.Default with { KeySizeBytes = 16 },
        CryptographyOptions.Default with { SaltSizeBytes = 32 },
        CryptographyOptions.Default with { Pbkdf2Iterations = 300_000 },
        CryptographyOptions.Default with { BlobFormatVersion = 0x02 },
    };

    [Theory]
    [MemberData(nameof(DivergentProfiles))]
    public void Constructor_RejectsValidButNonPersistedProfile(CryptographyOptions options)
    {
        Action box = () => _ = new AesGcmCryptoBox(options);
        Action helper = () => CryptoBox.Seal(options, "x", Pin);

        box.Should().Throw<ArgumentOutOfRangeException>();
        helper.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Seal_WritesVersionFromOptions()
    {
        string sealedBlob = CryptoBox.Seal(CryptographyOptions.Default, "payload", Pin);

        Convert.FromBase64String(sealedBlob)[0].Should().Be(CryptographyOptions.Default.BlobFormatVersion);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(40)]
    [InlineData(46)]
    public void Open_RejectsTamperedHeaderTagOrCipher(int index)
    {
        byte[] packed = Convert.FromBase64String(CryptoBox.Seal(CryptographyOptions.Default, "payload", Pin));
        packed[index] ^= 0x01;

        Action act = () => CryptoBox.Open(CryptographyOptions.Default, Convert.ToBase64String(packed), Pin);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void SensitiveBuffer_ZeroesOnDispose()
    {
        byte[] raw = [1, 2, 3, 4];
        SensitiveBuffer buffer = new(raw);

        buffer.Dispose();

        raw.Should().OnlyContain(b => b == 0);
        Action read = () => _ = buffer.Span.Length;
        read.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void DeriveKey_ReturnsSelfWipingBuffer()
    {
        byte[] salt = new byte[CryptographyOptions.Default.SaltSizeBytes];
        SensitiveBuffer key = CryptoBox.DeriveKey(CryptographyOptions.Default, Pin, salt);
        using (key)
        {
            key.Length.Should().Be(CryptographyOptions.Default.KeySizeBytes);
        }

        Action read = () => _ = key.Span.Length;
        read.Should().Throw<ObjectDisposedException>();
    }
}
