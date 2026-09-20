// <copyright file="RsaOaepSha256UserDataEnrollAlgorithmTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.UserData;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class RsaOaepSha256UserDataEnrollAlgorithmTests
{
    private const string FixtureMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    // Pinned after first DeriveKeyPair on this BC seed path; change only with intentional break.
    private const string ExpectedSpkiFingerprintSha256Hex =
        "4b23a249439ab9c80705fc2785ec5625f3eb556f8632b054bd88008a5d04957d";

    private readonly RsaOaepSha256UserDataEnrollAlgorithm _enroll = new();

    [Fact]
    public void DeriveKeyPair_IsDeterministicForFixtureSeed()
    {
        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);

        using UserDataEnrollKeyPair a = _enroll.DeriveKeyPair(material.EnrollSeed);
        using UserDataEnrollKeyPair b = _enroll.DeriveKeyPair(material.EnrollSeed);

        a.AlgorithmId.Should().Be(UserDataConstants.EnrollAlgorithmRsaOaepSha256V1);
        a.PublicKeyPem.Should().Contain("BEGIN PUBLIC KEY");
        a.SpkiFingerprintSha256Hex.Should().Be(b.SpkiFingerprintSha256Hex);
        a.PublicKeyPem.Should().Be(b.PublicKeyPem);
        a.PrivateKeyPkcs8Der.ToArray().Should().Equal(b.PrivateKeyPkcs8Der.ToArray());
    }

    [Fact]
    public void DeriveKeyPair_FixtureFingerprint_IsPinned()
    {
        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);
        using UserDataEnrollKeyPair keys = _enroll.DeriveKeyPair(material.EnrollSeed);

        keys.SpkiFingerprintSha256Hex.Should().Be(ExpectedSpkiFingerprintSha256Hex);
    }

    [Fact]
    public void EncryptDecryptChallenge_RoundTrips()
    {
        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);
        using UserDataEnrollKeyPair keys = _enroll.DeriveKeyPair(material.EnrollSeed);

        byte[] challenge = RandomNumberGenerator.GetBytes(96);
        byte[] cipher = _enroll.EncryptChallenge(challenge, keys.PublicKeyPem);
        byte[] plain = _enroll.DecryptChallenge(cipher, keys);

        plain.Should().Equal(challenge);
    }

    [Fact]
    public void Dispose_ZerosPrivateKeyAccess()
    {
        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataEnrollKeyPair keys = _enroll.DeriveKeyPair(material.EnrollSeed);
        keys.Dispose();

        Action act = () => _ = keys.PrivateKeyPkcs8Der.Length;
        act.Should().Throw<ObjectDisposedException>();
    }
}
