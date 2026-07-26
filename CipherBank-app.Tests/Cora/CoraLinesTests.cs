// <copyright file="CoraLinesTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Cora;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Cora;

public class CoraLinesTests
{
    [Theory]
    [InlineData("convert", "Locked-in rate")]
    [InlineData("pay", "Rent, paid")]
    [InlineData("keys", "forgot password")]
    [InlineData("home", "Rates move")]
    public void For_ReturnsKnownScreenLine(string screen, string snippet)
    {
        CoraLines.For(screen).Should().Contain(snippet);
    }

    [Fact]
    public void For_UnknownScreen_FallsBack()
    {
        CoraLines.For("not-a-screen").Should().Be("CipherBank.");
    }
}
