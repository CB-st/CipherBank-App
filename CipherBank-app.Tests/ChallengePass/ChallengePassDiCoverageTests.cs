// <copyright file="ChallengePassDiCoverageTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Configuration;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.ChallengePass.Structures;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.V1;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CipherBank_app.Tests.ChallengePass;

/// <summary>
/// Coverage for DI registration and locked key-source paths.
/// Use: High (CI). Scope: ChallengePass module surface.
/// </summary>
public sealed class ChallengePassDiCoverageTests
{
    /// <summary>
    /// Proves AddChallengePassModule wires A1+A2 suites and resolves the catalog.
    /// Use: High. Scope: ChallengePassServiceCollectionExtensions.
    /// </summary>
    [Fact]
    public void AddChallengePassModule_RegistersA1AndA2Suites()
    {
        var services = new ServiceCollection();
        var algo = new X25519ChaChaSealAlgorithm();
        var template = new ChallengeIdNonceSha256Template();
        var client = new InMemorySessionChallengeClient(algo, template);
        var keyShare = new InMemoryPqKeyShareClient();
        var channel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);

        services.AddSingleton<ISessionChallengeClient>(client);
        services.AddSingleton<IPqKeyShareClient>(keyShare);
        services.AddSingleton<IPqChannelChallengeSource>(challenges);
        services.AddSingleton(channel);
        services.AddSingleton<IAccountKeySource>(new LockedAccountKeySource());
        services.AddChallengePassModule(ChallengePassServiceCollectionExtensions.SuiteA2Id);

        using ServiceProvider sp = services.BuildServiceProvider();
        IChallengePassCatalog catalog = sp.GetRequiredService<IChallengePassCatalog>();
        catalog.ActiveSuiteId.Should().Be(ChallengePassServiceCollectionExtensions.SuiteA2Id);
        catalog.AvailableSuiteIds.Should().Contain(ChallengePassServiceCollectionExtensions.SuiteA1Id);
        catalog.AvailableSuiteIds.Should().Contain(ChallengePassServiceCollectionExtensions.SuiteA2Id);
        catalog.Active.Structure.Should().BeOfType<PqChannelChallengePassStructure>();
        sp.GetRequiredService<ChallengePassSessionProofBuilder>().Should().NotBeNull();
        sp.GetRequiredService<LabSessionProofBuilder>().Should().NotBeNull();
    }

    /// <summary>
    /// Proves the embedded configuration binds to typed options and selects the compatibility suite.
    /// Use: High. Scope: ChallengePass configuration and DI composition.
    /// </summary>
    [Fact]
    public void AddChallengePassModule_BindsEmbeddedDefaults()
    {
        var services = new ServiceCollection();
        RegisterRequiredPorts(services);
        services.AddChallengePassModule(ChallengePassDefaultsConfiguration.Build());

        using ServiceProvider provider = services.BuildServiceProvider();
        IChallengePassCatalog catalog = provider.GetRequiredService<IChallengePassCatalog>();
        catalog.ActiveSuiteId.Should().Be(ChallengePassServiceCollectionExtensions.SuiteA1Id);
    }

    /// <summary>
    /// Proves direct configuration rejects an unknown suite before a provider can be built.
    /// Use: Low. Scope: ChallengePass registration guard.
    /// </summary>
    [Fact]
    public void AddChallengePassModule_RejectsUnknownDirectSuite()
    {
        var services = new ServiceCollection();
        Action register = () => services.AddChallengePassModule("retired-suite");
        register.Should().Throw<ArgumentException>().WithMessage("*not installed*");
    }

    /// <summary>
    /// Proves host overrides are validated when the catalog first consumes typed options.
    /// Use: High. Scope: ChallengePass startup validation.
    /// </summary>
    [Fact]
    public void AddChallengePassModule_RejectsUnknownConfiguredSuite()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ChallengePassOptions.SectionName}:ActiveSuiteId"] = "retired-suite",
            })
            .Build();
        var services = new ServiceCollection();
        RegisterRequiredPorts(services);
        services.AddChallengePassModule(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        Action resolve = () => provider.GetRequiredService<IChallengePassCatalog>();
        resolve.Should().Throw<OptionsValidationException>().WithMessage("*not installed*");
    }

    /// <summary>
    /// Proves LockedAccountKeySource fails closed until custody unlock is wired.
    /// Use: Medium. Scope: LockedAccountKeySource.
    /// </summary>
    [Fact]
    public void LockedAccountKeySource_ThrowsUntilUnlocked()
    {
        var source = new LockedAccountKeySource();
        var algo = new X25519ChaChaSealAlgorithm();
        Action a1 = () => source.RequireUnlockedKeyPair(algo);
        Action a2 = () => source.RequireHybridIdentity();
        a1.Should().Throw<InvalidOperationException>().WithMessage("*not unlocked*");
        a2.Should().Throw<InvalidOperationException>().WithMessage("*not unlocked*");
    }

    /// <summary>
    /// Proves ChannelSealAlgorithm delegates Seal/Open to the PQ channel and rejects DeriveKeyPair.
    /// Use: High. Scope: ChannelSealAlgorithm.
    /// </summary>
    [Fact]
    public void ChannelSealAlgorithm_DelegatesToChannel()
    {
        using var channel = new PqSymmetricChannel();
        var key = new byte[32];
        key.AsSpan().Fill(0x42);
        channel.SetChannelKey(key, "ks_test");
        var seal = new ChannelSealAlgorithm(channel);

        seal.AlgorithmId.Should().Be(channel.ChannelAlgorithmId);
        seal.PublicKeySize.Should().Be(0);
        seal.PrivateKeySize.Should().Be(0);
        Action derive = () => seal.DeriveKeyPair(key);
        derive.Should().Throw<NotSupportedException>();

        var cipher = seal.Seal("hello-channel"u8.ToArray(), ReadOnlySpan<byte>.Empty);
        var plain = seal.Open(cipher, ReadOnlySpan<byte>.Empty);
        plain.Should().Equal("hello-channel"u8.ToArray());
    }

    /// <summary>
    /// Proves ClearDeviceIdentity zeroes cached identity and forces a fresh key-share.
    /// Use: High. Scope: PqChannelChallengePassStructure.ClearDeviceIdentity.
    /// </summary>
    [Fact]
    public async Task Pq_clear_device_identity_forces_new_key_share()
    {
        var agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        using var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        var seal = new ChannelSealAlgorithm(deviceChannel);

        structure.StructureId.Should().Be(PqChannelChallengePassStructure.StructureIdValue);
        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, device, CancellationToken.None);
        keyShare.EstablishCount.Should().Be(1);

        structure.ClearDeviceIdentity();

        // ClearDeviceIdentity zeroes the cached identity buffers (same object as `device`).
        HybridPrivateIdentity device2 = agreement.DeriveIdentity(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, device2, CancellationToken.None);
        keyShare.EstablishCount.Should().Be(2);
    }

    /// <summary>
    /// Registers the ports that a host owns while leaving suite composition to the module.
    /// Use: Low (configuration test setup). Scope: this fixture.
    /// </summary>
    private static void RegisterRequiredPorts(IServiceCollection services)
    {
        var algorithm = new X25519ChaChaSealAlgorithm();
        var template = new ChallengeIdNonceSha256Template();
        var keyShare = new InMemoryPqKeyShareClient();
        services.AddSingleton<ISessionChallengeClient>(new InMemorySessionChallengeClient(algorithm, template));
        services.AddSingleton<IPqKeyShareClient>(keyShare);
        services.AddSingleton<IPqChannelChallengeSource>(new InMemoryPqChannelChallengeSource(keyShare));
        services.AddSingleton<IAccountKeySource>(new LockedAccountKeySource());
    }
}
