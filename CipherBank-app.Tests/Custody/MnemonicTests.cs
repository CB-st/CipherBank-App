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
}
