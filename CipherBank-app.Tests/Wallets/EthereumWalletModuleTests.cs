// <copyright file="EthereumWalletModuleTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using System.Numerics;
using CipherBank_app.Wallets;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Wallets;

public class EthereumWalletModuleTests
{
    private const string Address = "0x0000000000000000000000000000000000000001";

    private readonly EthereumWalletModule _module = new();

    /// <summary>
    /// ERC-681 value is wei. 0.1 ETH, 1 ETH, and 1 wei are exact integer counts.
    /// Use: High (receive QR). Scope: EthereumWalletModule.BuildReceiveUri.
    /// </summary>
    [Fact]
    public void BuildReceiveUri_EncodesNominalEthAsWei()
    {
        string oneTenth = BigInteger.Pow(10, 17).ToString(CultureInfo.InvariantCulture);
        string oneEth = BigInteger.Pow(10, 18).ToString(CultureInfo.InvariantCulture);
        string oneWei = "0." + new string('0', 17) + "1";

        ReceiveUri("0.1").Should().Be($"ethereum:{Address}?value={oneTenth}");
        ReceiveUri("1").Should().Be($"ethereum:{Address}?value={oneEth}");
        ReceiveUri(oneWei).Should().Be($"ethereum:{Address}?value=1");
        ReceiveUri(null).Should().Be($"ethereum:{Address}");
    }

    /// <summary>
    /// Negative amounts and more than 18 fractional digits are rejected.
    /// Use: High (receive QR). Scope: EthereumWalletModule.BuildReceiveUri.
    /// </summary>
    [Theory]
    [InlineData("-0.1")]
    [InlineData("-1")]
    [InlineData("1.0000000000000000001")]
    public void BuildReceiveUri_RejectsNegativeOrExcessScale(string amount)
    {
        Action act = () => ReceiveUri(amount);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Nineteen fractional digits is more precision than wei.
    /// Use: Medium (receive QR). Scope: EthereumWalletModule.BuildReceiveUri.
    /// </summary>
    [Fact]
    public void BuildReceiveUri_RejectsNineteenFractionalDigits()
    {
        string amount = "0." + new string('0', 18) + "1";
        Action act = () => ReceiveUri(amount);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private string ReceiveUri(string? amount)
        => _module.BuildReceiveUri(Address, amount, null, null).OriginalString;
}
