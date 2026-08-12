// <copyright file="MnemonicTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class MnemonicTests
{
    [Fact]
    public void Generate_IsValidTwelveWords()
    {
        string phrase = MnemonicHelper.Generate();
        MnemonicHelper.Validate(phrase).Should().BeTrue();
        MnemonicHelper.Words(phrase).Should().HaveCount(12);
    }

    [Fact]
    public void Entropy_RecoversKnownBip39Vector()
    {
        // BIP39: 128-bit zero entropy → fixed 12-word English mnemonic.
        const string phrase =
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        byte[] entropy = MnemonicHelper.Entropy(phrase);
        entropy.Should().HaveCount(16);
        entropy.Should().OnlyContain(b => b == 0);
    }

    [Fact]
    public void Entropy_IsStableAcrossNormalize()
    {
        string phrase = MnemonicHelper.Generate();
        byte[] a = MnemonicHelper.Entropy(phrase);
        byte[] b = MnemonicHelper.Entropy("  " + phrase.ToUpperInvariant() + "  ");
        a.Should().Equal(b);
    }

    /// <summary>
    /// Trezor BIP39 English vectors (non-zero bit patterns catch bit-order bugs).
    /// Use: High (custody). Scope: MnemonicHelper.Entropy.
    /// </summary>
    [Theory]
    [InlineData("legal winner thank year wave sausage worth useful legal winner thank yellow", "7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f7f")]
    [InlineData("letter advice cage absurd amount doctor acoustic avoid letter advice cage above", "80808080808080808080808080808080")]
    [InlineData("zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo zoo wrong", "ffffffffffffffffffffffffffffffff")]
    [InlineData("scheme spot photo card baby mountain device kick cradle pact join borrow", "c0ba5a8e914111210f2bd131f3d5e08d")]
    [InlineData(
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon art",
        "0000000000000000000000000000000000000000000000000000000000000000")]
    public void Entropy_MatchesKnownAnswer(string phrase, string entropyHex)
    {
        MnemonicHelper.Validate(phrase).Should().BeTrue();
        Convert.ToHexStringLower(MnemonicHelper.Entropy(phrase)).Should().Be(entropyHex);
    }

    [Fact]
    public void Validate_RejectsBadChecksum()
        => MnemonicHelper.Validate("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon").Should().BeFalse();
}
