// <copyright file="MockUserDataClientTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.UserData;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class MockUserDataClientTests
{
    private const string FixtureMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Fact]
    public async Task EnrollChallengeOverwriteGrab_RoundTripsBlob()
    {
        IUserDataClient client = new MockUserDataClient();
        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);
        var enrollAlgo = new RsaOaepSha256UserDataEnrollAlgorithm();
        using UserDataEnrollKeyPair keys = enrollAlgo.DeriveKeyPair(material.EnrollSeed);

        UserDataEnrollResult enroll = await client.EnrollAsync("alice", keys.PublicKeyPem);
        enroll.Code.Should().Be(UserDataStatusCode.Ok);

        UserDataChallengeIssue challenge = await client.ChallengeAsync("alice", UserDataWireNames.TwoFaEmail);
        challenge.IsSuccess.Should().BeTrue();
        byte[] plain = enrollAlgo.DecryptChallenge(challenge.EncryptedChallenge!, keys);

        const string blob = "cGFjay1maXh0dXJl"; // arbitrary base64
        UserDataOverwriteResult put = await client.OverwriteAsync("alice", plain, blob);
        put.IsSuccess.Should().BeTrue();

        UserDataChallengeIssue challenge2 = await client.ChallengeAsync("alice", UserDataWireNames.TwoFaEmail);
        byte[] plain2 = enrollAlgo.DecryptChallenge(challenge2.EncryptedChallenge!, keys);
        UserDataGrabResult grab = await client.GrabAsync("alice", plain2);
        grab.IsSuccess.Should().BeTrue();
        grab.UserDataBlobBase64.Should().Be(blob);
    }

    [Fact]
    public async Task Enroll_DuplicateUsername_ReturnsExists()
    {
        IUserDataClient client = new MockUserDataClient();
        using UserDataKeyMaterial material = UserDataKeyDerivation.Derive(FixtureMnemonic);
        using UserDataEnrollKeyPair keys = new RsaOaepSha256UserDataEnrollAlgorithm()
            .DeriveKeyPair(material.EnrollSeed);

        await client.EnrollAsync("alice", keys.PublicKeyPem);
        UserDataEnrollResult second = await client.EnrollAsync("alice", keys.PublicKeyPem);
        second.Code.Should().Be(UserDataStatusCode.UsernameExists);
        second.IsSuccess.Should().BeTrue();
    }
}
