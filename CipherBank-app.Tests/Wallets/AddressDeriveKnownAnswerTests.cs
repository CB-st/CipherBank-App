// <copyright file="AddressDeriveKnownAnswerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Wallets;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Wallets;

/// <summary>Published BIP84 / BIP44 vectors for the all-"abandon ... about" seed.</summary>
public class AddressDeriveKnownAnswerTests
{
    private const string Mnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Theory]
    [InlineData("BTC", 0, "bc1qcr8te4kr609gcawutmrza0j4xv80jy8z306fyu", "m/84'/0'/0'/0/0")]
    [InlineData("ETH", 0, "0x9858EfFD232B4033E47d90003D41EC34EcaEda94", "m/44'/60'/0'/0/0")]
    public void DeriveRequired_MatchesPublishedVector(string symbol, int account, string address, string path)
    {
        DerivedAddress derived = AddressDerive.DeriveRequired(symbol, Mnemonic, account);

        derived.Address.Should().Be(address);
        derived.Path.Should().Be(path);
    }

    [Fact]
    public void DeriveRequired_AccountIndexChangesAddress()
    {
        DerivedAddress first = AddressDerive.DeriveRequired(AssetSymbol.Btc, Mnemonic, 0);
        DerivedAddress second = AddressDerive.DeriveRequired(AssetSymbol.Btc, Mnemonic, 1);

        second.Address.Should().NotBe(first.Address);
    }

    [Fact]
    public void DeriveRequired_ThrowsForNonDerivableAsset()
    {
        Action act = () => AddressDerive.DeriveRequired(AssetSymbol.Xmr, Mnemonic, 0);

        act.Should().Throw<InvalidOperationException>();
    }
}
