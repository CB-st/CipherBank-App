// <copyright file="HybridPqChannelTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.ChallengePass;

public sealed class HybridPqChannelTests
{
    [Fact]
    public void Hybrid_key_share_produces_matching_channel_keys()
    {
        var agreement = new HybridMlKemX25519Agreement();
        var entropy = RandomNumberGenerator.GetBytes(16);
        HybridPrivateIdentity device = agreement.DeriveIdentity(entropy);

        (PqKeyShareResponse response, var serverKey) = HybridMlKemX25519Agreement.CreateShareAsServer(device.ToPublic());
        var deviceKey = HybridMlKemX25519Agreement.CompleteAsDevice(device, response);

        deviceKey.Should().Equal(serverKey);
        deviceKey.Should().HaveCount(32);
        response.Algorithm.Should().Be(HybridMlKemX25519Agreement.KeyShareAlgorithmId);
    }

    [Fact]
    public void MlKem_encapsulate_decapsulate_round_trip()
    {
        (var pub, var priv) = MlKem768Provider.GenerateKeyPair();
        (var ct, var ss1) = MlKem768Provider.Encapsulate(pub);
        var ss2 = MlKem768Provider.Decapsulate(ct, priv);
        ss2.Should().Equal(ss1);
        ss1.Should().HaveCount(32);
    }

    [Fact]
    public async Task Pq_channel_challenge_pass_after_key_share()
    {
        var agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);

        var seal = new ChannelSealAlgorithm(deviceChannel);
        var body = await structure.BuildSessionOpenBodyWithIdentityAsync(
            seal,
            template,
            device,
            CancellationToken.None);

        SessionPassDto pass = Assert.IsType<SessionPassDto>(body);
        pass.Algorithm.Should().Be(HybridMlKemX25519Agreement.ChannelAlgorithmId);
        pass.PassCiphertext.Should().NotBeNullOrWhiteSpace();

        var json = System.Text.Json.JsonSerializer.Serialize(pass).ToLowerInvariant();
        json.Should().NotContain("mnemonic");
        json.Should().NotContain("seed");

        // Server can open the pass with the shared channel key.
        using var serverChannel = new PqSymmetricChannel();
        serverChannel.SetChannelKey(keyShare.LastChannelKey!, keyShare.LastKeyShareId!);
        var payload = serverChannel.Open(WireEncoding.FromWire(pass.PassCiphertext));
        payload.Should().HaveCount(32);
    }

    [Fact]
    public void Catalog_includes_a2_suite_id_constant()
        => ChallengePassServiceCollectionExtensions.SuiteA2Id.Should().Be("a2-hybrid-pq-channel-v1");

    [Fact]
    public async Task Pq_channel_re_establishes_when_device_identity_changes()
    {
        var agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity first = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));
        HybridPrivateIdentity second = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        var seal = new ChannelSealAlgorithm(deviceChannel);

        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, first, CancellationToken.None);

        var firstShareId = keyShare.LastKeyShareId!;
        keyShare.EstablishCount.Should().Be(1);

        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, second, CancellationToken.None);

        keyShare.EstablishCount.Should().Be(2);
        keyShare.LastKeyShareId.Should().NotBe(firstShareId);
    }

    [Fact]
    public async Task Pq_channel_concurrent_builds_do_not_fault()
    {
        var agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        var seal = new ChannelSealAlgorithm(deviceChannel);

        Task<object>[] builds = Enumerable.Range(0, 4)
            .Select(_ => structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, device, CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(builds);
        results.Should().AllSatisfy(r => Assert.IsType<SessionPassDto>(r));
    }

    /// <summary>
    /// Proves the production fused path (identity + channel under one gate) without SetDeviceIdentity.
    /// Use: High. Scope: BuildSessionOpenBodyWithIdentityAsync.
    /// </summary>
    [Fact]
    public async Task Pq_channel_fused_identity_build_without_prior_set()
    {
        var agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        var seal = new ChannelSealAlgorithm(deviceChannel);

        var body = await structure.BuildSessionOpenBodyWithIdentityAsync(
            seal,
            template,
            device,
            CancellationToken.None);

        SessionPassDto pass = Assert.IsType<SessionPassDto>(body);
        pass.Algorithm.Should().Be(HybridMlKemX25519Agreement.ChannelAlgorithmId);
        keyShare.EstablishCount.Should().Be(1);

        using var serverChannel = new PqSymmetricChannel();
        serverChannel.SetChannelKey(keyShare.LastChannelKey!, keyShare.LastKeyShareId!);
        var payload = serverChannel.Open(WireEncoding.FromWire(pass.PassCiphertext));
        payload.Should().HaveCount(32);
    }

    /// <summary>
    /// Proves ChallengePassSessionProofBuilder reaches A2 via the fused identity API.
    /// Use: High. Scope: production unlock path.
    /// </summary>
    [Fact]
    public async Task Proof_builder_a2_uses_fused_identity_path()
    {
        var agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        var seal = new ChannelSealAlgorithm(deviceChannel);

        var suite = new ChallengePassSuite(
            ChallengePassServiceCollectionExtensions.SuiteA2Id,
            seal,
            template,
            structure);
        var catalog = new ChallengePassCatalog([suite], suite.SuiteId);
        var account = new AccountKeyPair(device.X25519PublicKey, device.X25519PrivateKey);
        var builder = new ChallengePassSessionProofBuilder(
            catalog,
            new StaticAccountKeySource(account, device));

        var body = await builder.BuildOpenBodyAsync(CancellationToken.None);
        SessionPassDto pass = Assert.IsType<SessionPassDto>(body);
        pass.Algorithm.Should().Be(HybridMlKemX25519Agreement.ChannelAlgorithmId);
        keyShare.EstablishCount.Should().Be(1);
    }

    /// <summary>
    /// Same public X25519 identity under a new object must reuse the channel (no second key-share).
    /// Use: High. Scope: ApplyIdentityUnlocked channel-binding guard.
    /// </summary>
    [Fact]
    public async Task Pq_fused_same_pubkey_reuses_channel_without_new_key_share()
    {
        var agreement = new HybridMlKemX25519Agreement();
        var entropy = RandomNumberGenerator.GetBytes(16);
        HybridPrivateIdentity first = agreement.DeriveIdentity(entropy);
        HybridPrivateIdentity second = agreement.DeriveIdentity(entropy);

        first.X25519PublicKey.Should().Equal(second.X25519PublicKey);
        ReferenceEquals(first, second).Should().BeFalse();

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        using var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        var seal = new ChannelSealAlgorithm(deviceChannel);

        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, first, CancellationToken.None);
        keyShare.EstablishCount.Should().Be(1);

        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, second, CancellationToken.None);
        keyShare.EstablishCount.Should().Be(1);
    }

    /// <summary>
    /// Proves RequireHybridIdentity returns buffer copies so ClearDeviceIdentity cannot brick the fixture.
    /// Use: High. Scope: StaticAccountKeySource + PqChannelChallengePassStructure wipe contract.
    /// </summary>
    [Fact]
    public async Task Static_hybrid_source_survives_clear_after_fused_build()
    {
        var agreement = new HybridMlKemX25519Agreement();
        var device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));
        var account = new AccountKeyPair(device.X25519PublicKey.ToArray(), device.X25519PrivateKey.ToArray());
        var source = new StaticAccountKeySource(account, device);

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        using var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        var seal = new ChannelSealAlgorithm(deviceChannel);

        var handed = source.RequireHybridIdentity();
        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, handed, CancellationToken.None);
        structure.ClearDeviceIdentity();

        var again = source.RequireHybridIdentity();
        again.X25519PublicKey.Should().Equal(device.X25519PublicKey);
        again.X25519PrivateKey.Should().Equal(device.X25519PrivateKey);
        again.MlKemPublicKey.Should().Equal(device.MlKemPublicKey);
        again.MlKemPrivateKey.Should().Equal(device.MlKemPrivateKey);

        var secondBody = await structure.BuildSessionOpenBodyWithIdentityAsync(
            seal,
            template,
            again,
            CancellationToken.None);
        secondBody.Should().BeOfType<SessionPassDto>();
    }

    /// <summary>
    /// Proves a cancelled build-gate wait zeroes the unadopted hybrid identity before rethrowing.
    /// Use: High. Scope: BuildSessionOpenBodyWithIdentityCoreAsync cancel wipe.
    /// </summary>
    [Fact]
    public async Task Fused_build_cancelled_before_gate_wipes_incoming_identity()
    {
        var agreement = new HybridMlKemX25519Agreement();
        var identity = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));
        identity.X25519PrivateKey.Should().Contain(b => b != 0);
        identity.MlKemPrivateKey.Should().Contain(b => b != 0);

        var keyShare = new InMemoryPqKeyShareClient();
        var deviceChannel = new PqSymmetricChannel();
        var challenges = new InMemoryPqChannelChallengeSource(keyShare);
        var template = new ChallengeIdNonceSha256Template();
        using var structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        var seal = new ChannelSealAlgorithm(deviceChannel);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var act = async () => await structure.BuildSessionOpenBodyWithIdentityAsync(
            seal,
            template,
            identity,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        identity.X25519PrivateKey.Should().OnlyContain(b => b == 0);
        identity.MlKemPrivateKey.Should().OnlyContain(b => b == 0);
        identity.X25519PublicKey.Should().OnlyContain(b => b == 0);
        identity.MlKemPublicKey.Should().OnlyContain(b => b == 0);
    }
}
