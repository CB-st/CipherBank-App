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
        byte[] entropy = RandomNumberGenerator.GetBytes(16);
        HybridPrivateIdentity device = agreement.DeriveIdentity(entropy);

        (PqKeyShareResponse response, byte[] serverKey) = agreement.CreateShareAsServer(device.ToPublic());
        byte[] deviceKey = agreement.CompleteAsDevice(device, response);

        deviceKey.Should().Equal(serverKey);
        deviceKey.Should().HaveCount(32);
        response.Algorithm.Should().Be(HybridMlKemX25519Agreement.KeyShareAlgorithmId);
    }

    [Fact]
    public void MlKem_encapsulate_decapsulate_round_trip()
    {
        (byte[] pub, byte[] priv) = MlKem768Provider.GenerateKeyPair();
        (byte[] ct, byte[] ss1) = MlKem768Provider.Encapsulate(pub);
        byte[] ss2 = MlKem768Provider.Decapsulate(ct, priv);
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
        structure.SetDeviceIdentity(device);

        var seal = new ChannelSealAlgorithm(deviceChannel);
        object body = await structure.BuildSessionOpenBodyAsync(
            seal,
            template,
            new AccountKeyPair(device.X25519PublicKey, device.X25519PrivateKey),
            WireEncoding.ToWire(device.X25519PublicKey));

        var pass = Assert.IsType<SessionPassDto>(body);
        pass.Algorithm.Should().Be(HybridMlKemX25519Agreement.ChannelAlgorithmId);
        pass.PassCiphertext.Should().NotBeNullOrWhiteSpace();

        string json = System.Text.Json.JsonSerializer.Serialize(pass).ToLowerInvariant();
        json.Should().NotContain("mnemonic");
        json.Should().NotContain("seed");

        // Server can open the pass with the shared channel key.
        using var serverChannel = new PqSymmetricChannel();
        serverChannel.SetChannelKey(keyShare.LastChannelKey!, keyShare.LastKeyShareId!);
        byte[] payload = serverChannel.Open(WireEncoding.FromWire(pass.PassCiphertext));
        payload.Should().HaveCount(32);
    }

    [Fact]
    public void Catalog_includes_a2_suite_id_constant()
    {
        ChallengePassServiceCollectionExtensions.SuiteA2Id.Should().Be("a2-hybrid-pq-channel-v1");
    }
}
