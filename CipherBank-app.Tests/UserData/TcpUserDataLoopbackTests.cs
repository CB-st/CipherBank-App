// <copyright file="TcpUserDataLoopbackTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.UserData;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class TcpUserDataLoopbackTests
{
    private const string FixtureMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Fact]
    public async Task TcpClient_AgainstLoopbackServer_RoundTripsPackBlob()
    {
        var store = new InMemoryUserDataStore();
        var logic = new UserDataServiceLogic(store);
        await using var server = new UserDataLoopbackServer(logic);
        await server.StartAsync();

        IUserDataClient tcpClient = new UserDataClient(new TcpUserDataTransport(server.CreateClientOptions()));
        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);
        var enrollAlgo = new RsaOaepSha256UserDataEnrollAlgorithm();
        using UserDataEnrollKeyPair keys = enrollAlgo.DeriveKeyPair(material.EnrollSeed);

        (await tcpClient.EnrollAsync("alice", keys.PublicKeyPem)).Code.Should().Be(UserDataStatusCode.Ok);

        UserDataChallengeIssue challenge = await tcpClient.ChallengeAsync("alice", UserDataWireNames.TwoFaAuthenticator);
        challenge.IsSuccess.Should().BeTrue();
        byte[] plain = enrollAlgo.DecryptChallenge(challenge.EncryptedChallenge!, keys);

        UserDataPackWire pack = UserDataPackCodec.SealPack(
            "alice",
            contentVersion: 1,
            material.Kek,
            [new UserDataPlainBlock("prefs", UserDataBlockTypes.Prefs, """{"APPEARANCE":"dark"}""")]);
        string blob = UserDataPackCodec.EncodeBlob(pack);

        (await tcpClient.OverwriteAsync("alice", plain, blob)).IsSuccess.Should().BeTrue();

        UserDataChallengeIssue challenge2 = await tcpClient.ChallengeAsync("alice", UserDataWireNames.TwoFaAuthenticator);
        byte[] plain2 = enrollAlgo.DecryptChallenge(challenge2.EncryptedChallenge!, keys);
        UserDataGrabResult grab = await tcpClient.GrabAsync("alice", plain2);
        grab.IsSuccess.Should().BeTrue();

        UserDataPackWire remote = UserDataPackCodec.DecodeBlob(grab.UserDataBlobBase64!);
        UserDataPackCodec.OpenPack(remote, "alice", material.Kek)["prefs"]
            .Should().Be("""{"APPEARANCE":"dark"}""");
    }

    [Fact]
    public async Task MockAndTcp_ShareStore_CrossSubstantiate()
    {
        var store = new InMemoryUserDataStore();
        var logic = new UserDataServiceLogic(store);
        IUserDataClient mock = new MockUserDataClient(logic);
        await using var server = new UserDataLoopbackServer(logic);
        await server.StartAsync();
        IUserDataClient tcp = new UserDataClient(new TcpUserDataTransport(server.CreateClientOptions()));

        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);
        using UserDataEnrollKeyPair keys = new RsaOaepSha256UserDataEnrollAlgorithm()
            .DeriveKeyPair(material.EnrollSeed);

        await mock.EnrollAsync("bob", keys.PublicKeyPem);
        UserDataChallengeIssue challenge = await tcp.ChallengeAsync("bob", UserDataWireNames.TwoFaEmail);
        challenge.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ProductionOptions_DefaultPort53809()
    {
        UserDataEndpointOptions.Production().Port.Should().Be(53809);
        UserDataEndpointOptions.Production().Host.Should().Be("internal.cipherbank.money");
        UserDataEndpointOptions.Production().PayloadMode.Should().Be(UserDataPayloadMode.MasterKeyEncrypted);
    }
}
