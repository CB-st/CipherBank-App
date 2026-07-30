// <copyright file="MnemonicTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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
        var phrase = MnemonicHelper.Generate();
        MnemonicHelper.Validate(phrase).Should().BeTrue();
        MnemonicHelper.Words(phrase).Should().HaveCount(12);
    }

    [Fact]
    public void Entropy_RecoversKnownBip39Vector()
    {
        // BIP39: 128-bit zero entropy → fixed 12-word English mnemonic.
        const string phrase =
            "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var entropy = MnemonicHelper.Entropy(phrase);
        entropy.Should().HaveCount(16);
        entropy.Should().OnlyContain(b => b == 0);
    }

    [Fact]
    public void Entropy_IsStableAcrossNormalize()
    {
        var phrase = MnemonicHelper.Generate();
        var a = MnemonicHelper.Entropy(phrase);
        var b = MnemonicHelper.Entropy("  " + phrase.ToUpperInvariant() + "  ");
        a.Should().Equal(b);
    }
}
