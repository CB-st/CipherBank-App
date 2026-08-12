// <copyright file="CryptoBoxTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class CryptoBoxTests
{
    [Fact]
    public void SealOpen_RoundTripsPlaintext()
    {
        string sealedBlob = CryptoBox.Seal("alpha beta gamma", "123456");
        CryptoBox.Open(sealedBlob, "123456").Should().Be("alpha beta gamma");
    }

    [Fact]
    public void Open_WithWrongPin_Throws()
    {
        string sealedBlob = CryptoBox.Seal("secret", "123456");
        Action act = () => CryptoBox.Open(sealedBlob, "000000");
        act.Should().Throw<Exception>();
    }
}
