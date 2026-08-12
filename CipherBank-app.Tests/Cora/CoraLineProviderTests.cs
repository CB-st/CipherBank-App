// <copyright file="CoraLineProviderTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Cora;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace CipherBank_app.Tests.Cora;

public class CoraLineProviderTests
{
    private readonly CoraLineProvider _provider = CreateProvider();

    [Theory]
    [InlineData("convert", "Locked-in rate")]
    [InlineData("pay", "Rent, paid")]
    [InlineData("keys", "forgot password")]
    [InlineData("home", "Rates move")]
    public void GetLine_ReturnsKnownScreenLine(string screen, string snippet) => _provider.GetLine(screen).Should().Contain(snippet);

    [Fact]
    public void GetLine_UnknownScreen_FallsBack() => _provider.GetLine("not-a-screen").Should().Be("CipherBank.");

    private static CoraLineProvider CreateProvider()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();
        CoraOptions options = configuration.GetSection(CoraOptions.SectionName).Get<CoraOptions>()!;
        return new CoraLineProvider(Options.Create(options));
    }
}
