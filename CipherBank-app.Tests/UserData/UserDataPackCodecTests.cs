// <copyright file="UserDataPackCodecTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.UserData;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class UserDataPackCodecTests
{
    private const string FixtureMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    private const string Username = "alice";

    // AES-GCM vectors: zero nonce + fixture KEK + AAD for alice/prefs/prefs/v1.
    private const string ExpectedZeroNonceTagB64 = "VG+E7OtAqIML1QgpsCaB+g==";

    private const string ExpectedZeroNonceCipherB64 = "MdzUOSWkOwN15+hJB/NkSyMpq3pu";

    [Fact]
    public void SealOpenPack_RoundTripsPrefsJson()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        const string prefsJson = """{"APPEARANCE":"dark","BASE_CURRENCY":"USD"}""";

        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 3,
            keys.Kek,
            [new UserDataPlainBlock(UserDataBlockTypes.Prefs, UserDataBlockTypes.Prefs, prefsJson)]);

        pack.Format.Should().Be(UserDataConstants.PackFormat);
        pack.ContentVersion.Should().Be(3u);
        pack.UsernameHashPrefix.Should().Be("2bd806c9");
        pack.Blocks.Should().HaveCount(1);

        Dictionary<string, string> opened = UserDataPackCodec.OpenPack(pack, Username, keys.Kek);
        opened[UserDataBlockTypes.Prefs].Should().Be(prefsJson);
    }

    [Fact]
    public void EncodeDecodeBlob_PreservesEnvelope()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            keys.Kek,
            [new UserDataPlainBlock("prefs", UserDataBlockTypes.Prefs, "{}")]);

        string blob = UserDataPackCodec.EncodeBlob(pack);
        UserDataPackWire parsed = UserDataPackCodec.DecodeBlob(blob);

        parsed.ContentVersion.Should().Be(1u);
        UserDataPackCodec.OpenPack(parsed, Username, keys.Kek)["prefs"].Should().Be("{}");
    }

    [Fact]
    public void OpenBlock_WrongAadUsername_Throws()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            keys.Kek,
            [new UserDataPlainBlock("prefs", UserDataBlockTypes.Prefs, "{}")]);

        Action act = () => UserDataPackCodec.OpenPack(pack, "bob", keys.Kek);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void OpenBlock_WrongContentVersion_Throws()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        string hash = UserDataUsernameHash.HashHex(Username);
        UserDataBlockWire block = UserDataPackCodec.SealBlock(
            new UserDataPlainBlock("prefs", UserDataBlockTypes.Prefs, "{}"),
            keys.Kek,
            hash,
            contentVersion: 1);

        Action act = () => UserDataPackCodec.OpenBlock(block, keys.Kek, hash, contentVersion: 2);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void SealBlockWithNonce_MatchesPinnedCiphertext()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        string hash = UserDataUsernameHash.HashHex(Username);
        byte[] nonce = new byte[12];
        UserDataBlockWire block = UserDataPackCodec.SealBlockWithNonce(
            new UserDataPlainBlock("prefs", UserDataBlockTypes.Prefs, """{"APPEARANCE":"dark"}"""),
            keys.Kek,
            hash,
            contentVersion: 1,
            nonce);

        block.NonceBase64.Should().Be("AAAAAAAAAAAAAAAA");
        block.TagBase64.Should().Be(ExpectedZeroNonceTagB64);
        block.CiphertextBase64.Should().Be(ExpectedZeroNonceCipherB64);

        UserDataPackCodec.OpenBlock(block, keys.Kek, hash, 1)
            .Should().Be("""{"APPEARANCE":"dark"}""");
    }
}
