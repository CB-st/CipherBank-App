// <copyright file="NetworkOptionsTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

public sealed class NetworkOptionsTests
{
    [Fact]
    public void Default_IncludesSandboxProductionDevelopmentAndLocal()
    {
        NetworkOptions options = NetworkOptions.Default;

        options.DefaultEnvironment.Should().Be("Sandbox");
        options.Environments.Keys.Should().BeEquivalentTo(
            ["Production", "Sandbox", "Development", "Local"]);
        options.Resolve("Sandbox").ApiBase.Should().Contain("sandbox");
        options.Resolve("Production").StreamEndpoint.Should().StartWith("wss://");
        options.Resolve("Local").ApiBase.Should().Contain("localhost");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_BlankEnvironment_UsesDefaultEnvironment(string? environment)
    {
        NetworkEnvironmentOptions endpoints = NetworkOptions.Default.Resolve(environment);

        endpoints.Should().BeSameAs(NetworkOptions.Default.Environments["Sandbox"]);
    }

    [Fact]
    public void Resolve_UnknownEnvironment_FallsBackToDefaultEnvironment()
    {
        NetworkEnvironmentOptions endpoints = NetworkOptions.Default.Resolve("not-a-real-env");

        endpoints.Should().BeSameAs(NetworkOptions.Default.Environments["Sandbox"]);
    }

    [Fact]
    public void Resolve_MissingDefault_ReturnsEmptyEndpoints()
    {
        NetworkOptions options = new()
        {
            DefaultEnvironment = "Missing",
        };
        options.Environments["Other"] = new NetworkEnvironmentOptions { ApiBase = "https://other.test" };

        NetworkEnvironmentOptions endpoints = options.Resolve("also-missing");

        endpoints.ApiBase.Should().BeEmpty();
        endpoints.PublicApiBase.Should().BeEmpty();
        endpoints.StreamEndpoint.Should().BeEmpty();
    }

    [Fact]
    public void EmbeddedDefaults_BindNetworkSection()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();
        NetworkOptions? bound = configuration
            .GetSection(NetworkOptions.SectionName)
            .Get<NetworkOptions>();

        bound.Should().NotBeNull();
        bound!.Environments.Should().NotBeEmpty();
        bound.Resolve(bound.DefaultEnvironment).ApiBase.Should().NotBeNullOrWhiteSpace();
    }
}
