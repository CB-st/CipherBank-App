// <copyright file="UserDataKeyDerivationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.UserData;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class UserDataKeyDerivationTests
{
    // BIP39 zero-entropy English vector (test-only; never a funded wallet).
    private const string FixtureMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    private const string ExpectedKekHex =
        "7a820e2ef0b659c68c3f9b447f04ab25df9ba7df6d64cd08696a4d9ac047e3a2";

    private const string ExpectedEnrollSeedHex =
        "06ede38b5ef7f042787f0debf77fe9635d018b79d86a39b3c2664dadbea912595a7fab08f5529edd295d369cfd9d2aa06351c9e5029db2c2141167f2344a058a";

    [Fact]
    public void Derive_IsStableAcrossNormalize()
    {
        using UserDataKeyMaterial a = UserDataKeyDerivation.Derive(FixtureMnemonic);
        using UserDataKeyMaterial b = UserDataKeyDerivation.Derive(
            "  ABANDON ABANDON ABANDON ABANDON ABANDON ABANDON ABANDON ABANDON ABANDON ABANDON ABANDON ABOUT  ");

        Convert.ToHexString(a.Kek).Should().BeEquivalentTo(Convert.ToHexString(b.Kek));
        Convert.ToHexString(a.EnrollSeed).Should().BeEquivalentTo(Convert.ToHexString(b.EnrollSeed));
    }

    [Fact]
    public void Derive_FixtureKekAndEnrollSeed_ArePinned()
    {
        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);

        Convert.ToHexString(material.Kek).ToLowerInvariant().Should().Be(ExpectedKekHex);
        Convert.ToHexString(material.EnrollSeed).ToLowerInvariant().Should().Be(ExpectedEnrollSeedHex);
    }

    [Fact]
    public void Dispose_PreventsFurtherAccess()
    {
        UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);
        material.Dispose();

        Action read = () => _ = material.Kek.Length;
        read.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Derive_InvalidMnemonic_Throws()
    {
        Action act = () => UserDataKeyDerivation.Derive("not a real mnemonic phrase at all here");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UsernameHash_AlicePrefix_IsPinned()
    {
        UserDataUsernameHash.HashHex("Alice")
            .Should().Be("2bd806c97f0e00af1a1fc3328fa763a9269723c8db8fac4f93af71db186d6e90");
        UserDataUsernameHash.HashPrefix("Alice").Should().Be("2bd806c9");
    }
}
