// <copyright file="MoneroWalletModuleTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Wallets;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Wallets;

/// <summary>Monero prefix-specific payload lengths and checksums across all networks.</summary>
public sealed class MoneroWalletModuleTests
{
    // Fixed vectors use deterministic payload bytes and independently computed Keccak checksums.
    [Theory]
    [InlineData("41fKjWJQySv2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8JuzEJT", true)]
    [InlineData("4BMzkK7uaiS2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscnaBkMV84ECzEi9DCKTnD", true)]
    [InlineData("82VU4sxFZsL2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8Fo6y6t", true)]
    [InlineData("41fKjWJQySv2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna1YzuNGETZ", false)]
    [InlineData("4BMzkK7uaiS2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8EFWhGg", false)]
    [InlineData("41fKjWJQySv2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscnaBkMV84ECzEi98z528J", false)]
    [InlineData("9sCsDkxgFp22LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8H1KgRm", true)]
    [InlineData("A2uYEZnAs5Y2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscnaBkMV84ECzEi9Ce7dPs", true)]
    [InlineData("BYDbMq9cLVC2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8JguiBH", true)]
    [InlineData("9sCsDkxgFp22LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna1Yzoaoroz", false)]
    [InlineData("A2uYEZnAs5Y2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8FcLpMC", false)]
    [InlineData("9sCsDkxgFp22LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscnaBkMV84ECzEi9DUq8ei", false)]
    [InlineData("51sMpMDNd422LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8JjsPTq", true)]
    [InlineData("5Ba2qA2sEKY2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscnaBkMV84ECzEi9DJbsA3", true)]
    [InlineData("72HRz33HvGE2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8KpcMs6", true)]
    [InlineData("51sMpMDNd422LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna1Yzu7bRPy", false)]
    [InlineData("5Ba2qA2sEKY2LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscna8FRanJs", false)]
    [InlineData("51sMpMDNd422LxFSMtsQ5k3gsHPkECmXt52nKM8ZY8z26NhMJWtsWSA7icPFuECstJ94XRDHZYFLSAQSTAftscnaBkMV84ECzEi9CE3fg3", false)]
    public void IsValidAddress_RequiresLengthForPrefix(string address, bool expected)
    {
        MoneroWalletModule module = new MoneroWalletModule();

        module.IsValidAddress(address).Should().Be(expected);
    }
}
