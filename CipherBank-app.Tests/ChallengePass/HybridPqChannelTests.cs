// <copyright file="HybridPqChannelTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        byte[] entropy = RandomNumberGenerator.GetBytes(16);
        HybridPrivateIdentity device = agreement.DeriveIdentity(entropy);

        (PqKeyShareResponse response, byte[]? serverKey) = HybridMlKemX25519Agreement.CreateShareAsServer(device.ToPublic());
        byte[] deviceKey = HybridMlKemX25519Agreement.CompleteAsDevice(device, response);

        deviceKey.Should().Equal(serverKey);
        deviceKey.Should().HaveCount(32);
        response.Algorithm.Should().Be(HybridMlKemX25519Agreement.KeyShareAlgorithmId);
    }

    [Fact]
    public void MlKem_encapsulate_decapsulate_round_trip()
    {
        (byte[]? pub, byte[]? priv) = MlKem768Provider.GenerateKeyPair();
        (byte[]? ct, byte[]? ss1) = MlKem768Provider.Encapsulate(pub);
        byte[] ss2 = MlKem768Provider.Decapsulate(ct, priv);
        ss2.Should().Equal(ss1);
        ss1.Should().HaveCount(32);
    }

    [Fact]
    public async Task Pq_channel_challenge_pass_after_key_share()
    {
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);

        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);
        object body = await structure.BuildSessionOpenBodyWithIdentityAsync(
            seal,
            template,
            device,
            CancellationToken.None);

        SessionPassDto pass = Assert.IsType<SessionPassDto>(body);
        pass.Algorithm.Should().Be(HybridMlKemX25519Agreement.ChannelAlgorithmId);
        pass.PassCiphertext.Should().NotBeNullOrWhiteSpace();

        string json = System.Text.Json.JsonSerializer.Serialize(pass).ToLowerInvariant();
        json.Should().NotContain("mnemonic");
        json.Should().NotContain("seed");

        // Server can open the pass with the shared channel key.
        using PqSymmetricChannel serverChannel = new PqSymmetricChannel();
        serverChannel.SetChannelKey(keyShare.LastChannelKey!, keyShare.LastKeyShareId!);
        byte[] payload = serverChannel.Open(WireEncoding.FromWire(pass.PassCiphertext));
        payload.Should().HaveCount(32);
    }

    [Fact]
    public void Catalog_includes_a2_suite_id_constant()
        => ChallengePassServiceCollectionExtensions.SuiteA2Id.Should().Be("a2-hybrid-pq-channel-v1");

    [Fact]
    public async Task Pq_channel_re_establishes_when_device_identity_changes()
    {
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity first = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));
        HybridPrivateIdentity second = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);

        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, first, CancellationToken.None);

        string firstShareId = keyShare.LastKeyShareId!;
        keyShare.EstablishCount.Should().Be(1);

        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, second, CancellationToken.None);

        keyShare.EstablishCount.Should().Be(2);
        keyShare.LastKeyShareId.Should().NotBe(firstShareId);
    }

    [Fact]
    public async Task Pq_channel_concurrent_builds_do_not_fault()
    {
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);

        Task<object>[] builds = Enumerable.Range(0, 4)
            .Select(_ => structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, device, CancellationToken.None))
            .ToArray();

        object[] results = await Task.WhenAll(builds);
        results.Should().AllSatisfy(r => Assert.IsType<SessionPassDto>(r));
    }

    /// <summary>
    /// Proves the production fused path (identity + channel under one gate) without SetDeviceIdentity.
    /// Use: High. Scope: BuildSessionOpenBodyWithIdentityAsync.
    /// </summary>
    [Fact]
    public async Task Pq_channel_fused_identity_build_without_prior_set()
    {
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);

        object body = await structure.BuildSessionOpenBodyWithIdentityAsync(
            seal,
            template,
            device,
            CancellationToken.None);

        SessionPassDto pass = Assert.IsType<SessionPassDto>(body);
        pass.Algorithm.Should().Be(HybridMlKemX25519Agreement.ChannelAlgorithmId);
        keyShare.EstablishCount.Should().Be(1);

        using PqSymmetricChannel serverChannel = new PqSymmetricChannel();
        serverChannel.SetChannelKey(keyShare.LastChannelKey!, keyShare.LastKeyShareId!);
        byte[] payload = serverChannel.Open(WireEncoding.FromWire(pass.PassCiphertext));
        payload.Should().HaveCount(32);
    }

    /// <summary>
    /// Proves ChallengePassSessionProofBuilder reaches A2 via the fused identity API.
    /// Use: High. Scope: production unlock path.
    /// </summary>
    [Fact]
    public async Task Proof_builder_a2_uses_fused_identity_path()
    {
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);

        ChallengePassSuite suite = new ChallengePassSuite(
            ChallengePassServiceCollectionExtensions.SuiteA2Id,
            seal,
            template,
            structure);
        ChallengePassCatalog catalog = new ChallengePassCatalog([suite], suite.SuiteId);
        AccountKeyPair account = new AccountKeyPair(device.X25519PublicKey, device.X25519PrivateKey);
        ChallengePassSessionProofBuilder builder = new ChallengePassSessionProofBuilder(
            catalog,
            new StaticAccountKeySource(account, device));

        object body = await builder.BuildOpenBodyAsync(CancellationToken.None);
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
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        byte[] entropy = RandomNumberGenerator.GetBytes(16);
        HybridPrivateIdentity first = agreement.DeriveIdentity(entropy);
        HybridPrivateIdentity second = agreement.DeriveIdentity(entropy);

        first.X25519PublicKey.Should().Equal(second.X25519PublicKey);
        ReferenceEquals(first, second).Should().BeFalse();

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        using PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);

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
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity device = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));
        AccountKeyPair account = new AccountKeyPair(device.X25519PublicKey.ToArray(), device.X25519PrivateKey.ToArray());
        StaticAccountKeySource source = new StaticAccountKeySource(account, device);

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        using PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);

        HybridPrivateIdentity handed = source.RequireHybridIdentity();
        await structure.BuildSessionOpenBodyWithIdentityAsync(seal, template, handed, CancellationToken.None);
        structure.ClearDeviceIdentity();

        HybridPrivateIdentity again = source.RequireHybridIdentity();
        again.X25519PublicKey.Should().Equal(device.X25519PublicKey);
        again.X25519PrivateKey.Should().Equal(device.X25519PrivateKey);
        again.MlKemPublicKey.Should().Equal(device.MlKemPublicKey);
        again.MlKemPrivateKey.Should().Equal(device.MlKemPrivateKey);

        object secondBody = await structure.BuildSessionOpenBodyWithIdentityAsync(
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
        HybridMlKemX25519Agreement agreement = new HybridMlKemX25519Agreement();
        HybridPrivateIdentity identity = agreement.DeriveIdentity(RandomNumberGenerator.GetBytes(16));
        identity.X25519PrivateKey.Should().Contain(b => b != 0);
        identity.MlKemPrivateKey.Should().Contain(b => b != 0);

        InMemoryPqKeyShareClient keyShare = new InMemoryPqKeyShareClient();
        PqSymmetricChannel deviceChannel = new PqSymmetricChannel();
        InMemoryPqChannelChallengeSource challenges = new InMemoryPqChannelChallengeSource(keyShare);
        ChallengeIdNonceSha256Template template = new ChallengeIdNonceSha256Template();
        using PqChannelChallengePassStructure structure = new PqChannelChallengePassStructure(keyShare, deviceChannel, challenges);
        ChannelSealAlgorithm seal = new ChannelSealAlgorithm(deviceChannel);

        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();
        Func<Task<object>> act = async () => await structure.BuildSessionOpenBodyWithIdentityAsync(
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
