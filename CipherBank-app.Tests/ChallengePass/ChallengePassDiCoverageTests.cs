// <copyright file="ChallengePassDiCoverageTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Algorithms;
using CipherBank_app.ChallengePass.Configuration;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.ChallengePass.Structures;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.Custody;
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
        ServiceCollection services = new ServiceCollection();
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        InMemorySessionChallengeClient client = new InMemorySessionChallengeClient(algo, template);
        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel channel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);

        services.AddSingleton<ISessionChallengeClient>(client);
        services.AddSingleton<IPqKeyShareClient>(keyShare);
        services.AddSingleton<IPqChannelChallengeSource>(challenges);
        services.AddSingleton(channel);
        services.AddSingleton<IAccountKeySource>(new LockedAccountKeySource());
        RegisterCustody(services);
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
        ServiceCollection services = new ServiceCollection();
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
        ServiceCollection services = new ServiceCollection();
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
        ServiceCollection services = new ServiceCollection();
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
        LockedAccountKeySource source = new LockedAccountKeySource();
        X25519ChaChaSealAlgorithm algo = new X25519ChaChaSealAlgorithm();
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
        using PqSymmetricChannel channel = new PqSymmetricChannel();
        byte[] key = new byte[32];
        key.AsSpan().Fill(0x42);
        channel.SetChannelKey(key, "ks_test");
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(channel);

        seal.AlgorithmId.Should().Be(channel.ChannelAlgorithmId);
        seal.PublicKeySize.Should().Be(0);
        seal.PrivateKeySize.Should().Be(0);
        Action derive = () => seal.DeriveKeyPair(key);
        derive.Should().Throw<NotSupportedException>();

        byte[] cipher = seal.Seal("hello-channel"u8.ToArray(), ReadOnlySpan<byte>.Empty);
        byte[] plain = seal.Open(cipher, ReadOnlySpan<byte>.Empty);
        plain.Should().Equal("hello-channel"u8.ToArray());
    }

    /// <summary>
    /// Proves ClearDeviceIdentity zeroes cached identity and forces a fresh key-share.
    /// Use: High. Scope: PqChannelChallengePassStructure.ClearDeviceIdentity.
    /// </summary>
    [Fact]
    public async Task Pq_clear_device_identity_forces_new_key_share()
    {
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        using PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);

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
    /// Proves the ChallengePass composition root clears A2 identity on ICustodyService.Locked.
    /// Use: High. Scope: ChallengePassServiceCollectionExtensions custody lock wiring.
    /// </summary>
    [Fact]
    public async Task AddChallengePassModule_ClearsA2IdentityOnCustodyLock()
    {
        ServiceCollection services = new ServiceCollection();
        RegisterRequiredPorts(services);
        services.AddChallengePassModule(ChallengePassServiceCollectionExtensions.SuiteA2Id);

        using ServiceProvider sp = services.BuildServiceProvider();
        ICustodyService custody = sp.GetRequiredService<ICustodyService>();
        PqChannelChallengePassStructure structure = sp.GetRequiredService<PqChannelChallengePassStructure>();
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(sp.GetRequiredService<IPqChannel>());
        ChallengeIdNonceSha256Template template = sp.GetRequiredService<ChallengeIdNonceSha256Template>();
        InMemoryPqKeyShareClient keyShare = (InMemoryPqKeyShareClient)sp.GetRequiredService<IPqKeyShareClient>();

        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, device, CancellationToken.None);
        keyShare.EstablishCount.Should().Be(1);

        custody.Lock();

        HybridPrivateIdentity device2 = agreement.DeriveIdentity(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, device2, CancellationToken.None);
        keyShare.EstablishCount.Should().Be(2, "custody lock must clear A2 so the next build re-shares");
    }

    /// <summary>
    /// Registers the ports that a host owns while leaving suite composition to the module.
    /// Use: Low (configuration test setup). Scope: this fixture.
    /// </summary>
    private static void RegisterRequiredPorts(IServiceCollection services)
    {
        X25519ChaChaSealAlgorithm algorithm = new X25519ChaChaSealAlgorithm();
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        services.AddSingleton<ISessionChallengeClient>(new InMemorySessionChallengeClient(algorithm, template));
        services.AddSingleton<IPqKeyShareClient>(keyShare);
        services.AddSingleton<IPqChannelChallengeSource>(new InMemoryPqChannelChallengeSource(keyShare));
        services.AddSingleton<IAccountKeySource>(new LockedAccountKeySource());
        RegisterCustody(services);
    }

    /// <summary>
    /// Registers an in-memory custody stack so A2 DI can subscribe to Locked.
    /// Use: Low (DI fixtures). Scope: ChallengePassDiCoverageTests.
    /// </summary>
    private static void RegisterCustody(IServiceCollection services)
    {
        MemStore store = new MemStore();
        services.AddSingleton<ISecureStore>(store);
        services.AddSingleton<IPinService>(new PinService(store));
        services.AddSingleton<ICustodyService, CustodyService>();
    }

    /// <summary>In-memory secure store for DI lock-wiring fixtures. Use: Low. Scope: this test class.</summary>
    private sealed class MemStore : ISecureStore
    {
        private readonly Dictionary<string, string> _data = new Dictionary<string, string>(StringComparer.Ordinal);

        public Task SetAsync(string key, string value)
        {
            _data[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
            => Task.FromResult(_data.TryGetValue(key, out string? value) ? value : null);

        public Task RemoveAsync(string key)
        {
            _data.Remove(key);
            return Task.CompletedTask;
        }
    }
}
