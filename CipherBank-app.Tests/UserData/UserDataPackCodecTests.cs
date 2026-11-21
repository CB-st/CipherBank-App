// <copyright file="UserDataPackCodecTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CipherBank_app.UserData;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class UserDataPackCodecTests
{
    private const string FixtureMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    private const string Username = "alice";

    private const string NullFormatPackJson =
        """{"format":null,"content_version":1,"username_hash_prefix":"2bd806c9","blocks":[]}""";

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

        IReadOnlyDictionary<string, string> opened = UserDataPackCodec.OpenPack(pack, Username, keys.Kek).Blocks;
        opened[UserDataBlockTypes.Prefs].Should().Be(prefsJson);
    }

    [Fact]
    public void OpenPack_AllBlocksAuthenticate_IsComplete()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            keys.Kek,
            [new UserDataPlainBlock(UserDataBlockTypes.Prefs, UserDataBlockTypes.Prefs, "{}")]);

        UserDataPackOpenResult result = UserDataPackCodec.OpenPack(pack, Username, keys.Kek);

        result.IsComplete.Should().BeTrue();
        result.FailedBlocks.Should().BeEmpty();
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
        UserDataPackCodec.OpenPack(parsed, Username, keys.Kek).Blocks["prefs"].Should().Be("{}");
    }

    [Fact]
    public void DecodeBlob_JsonNullFormat_ThrowsCryptographicException()
    {
        string blob = Convert.ToBase64String(Encoding.UTF8.GetBytes(NullFormatPackJson));

        Assert.Throws<CryptographicException>(() => UserDataPackCodec.DecodeBlob(blob));
    }

    [Fact]
    public void OpenPack_JsonNullFormat_ThrowsCryptographicException()
    {
        UserDataPackWire pack = Assert.IsType<UserDataPackWire>(JsonSerializer.Deserialize<UserDataPackWire>(NullFormatPackJson));
        byte[] kek = new byte[UserDataConstants.KekLength];

        Assert.Throws<CryptographicException>(() => UserDataPackCodec.OpenPack(pack, Username, kek));
    }

    [Fact]
    public void OpenPack_DecodedNullUsernameHashPrefix_ThrowsCryptographicException()
    {
        const string json =
            """{"format":"cipherbank-userdata-pack-v1","content_version":1,"username_hash_prefix":null,"blocks":[]}""";
        string blob = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        UserDataPackWire pack = UserDataPackCodec.DecodeBlob(blob);
        Assert.Null(pack.UsernameHashPrefix);
        byte[] kek = new byte[UserDataConstants.KekLength];

        Assert.Throws<CryptographicException>(() => UserDataPackCodec.OpenPack(pack, Username, kek));
    }

    [Fact]
    public void OpenPack_DecodedNullAlgorithm_ReportsAuthenticationFailed()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = DecodePackWithBlockField("alg", null, keys.Kek);

        UserDataPackOpenResult result = UserDataPackCodec.OpenPack(pack, Username, keys.Kek);

        result.Blocks.Should().BeEmpty();
        result.FailedBlocks.Should().ContainSingle().Which.Should().Be(
            new UserDataBlockFailure("prefs", UserDataBlockTypes.Prefs, UserDataBlockFailureReason.AuthenticationFailed));
    }

    [Theory]
    [InlineData("alg", null)]
    [InlineData("id", null)]
    [InlineData("id", "   ")]
    [InlineData("type", null)]
    [InlineData("type", "   ")]
    public void CipherOpen_DecodedInvalidBlockHeader_ThrowsCryptographicException(string field, string? value)
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = DecodePackWithBlockField(field, value, keys.Kek);
        UserDataBlockWire block = Assert.IsType<UserDataBlockWire>(Assert.Single(pack.Blocks));
        AesGcmUserDataBlockCipher cipher = new();
        string hash = UserDataUsernameHash.HashHex(Username);

        Assert.Throws<CryptographicException>(() => cipher.Open(block, keys.Kek, hash, pack.ContentVersion));
    }

    [Theory]
    [InlineData("nonce_b64")]
    [InlineData("tag_b64")]
    [InlineData("ciphertext_b64")]
    public void CipherOpen_DecodedNullBase64Field_ThrowsArgumentNullException(string field)
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = DecodePackWithBlockField(field, null, keys.Kek);
        UserDataBlockWire block = Assert.IsType<UserDataBlockWire>(Assert.Single(pack.Blocks));
        AesGcmUserDataBlockCipher cipher = new();
        string hash = UserDataUsernameHash.HashHex(Username);

        Assert.Throws<ArgumentNullException>(() => cipher.Open(block, keys.Kek, hash, pack.ContentVersion));
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
    public void OpenPack_SkipsAuthFailedBlock_KeepsGoodBlocks()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            keys.Kek,
            [
                new UserDataPlainBlock(UserDataBlockTypes.Prefs, UserDataBlockTypes.Prefs, """{"ok":true}"""),
                new UserDataPlainBlock(UserDataBlockTypes.AppConfig, UserDataBlockTypes.AppConfig, """{"cfg":1}"""),
            ]);

        UserDataBlockWire tamperedBlock = Assert.IsType<UserDataBlockWire>(pack.Blocks[1]);
        string ciphertextBase64 = Assert.IsType<string>(tamperedBlock.CiphertextBase64);
        byte[] cipher = Convert.FromBase64String(ciphertextBase64);
        cipher[0] ^= 0xFF;
        tamperedBlock.CiphertextBase64 = Convert.ToBase64String(cipher);

        UserDataPackOpenResult result = UserDataPackCodec.OpenPack(pack, Username, keys.Kek);
        IReadOnlyDictionary<string, string> opened = result.Blocks;
        opened.Should().ContainKey(UserDataBlockTypes.Prefs);
        opened[UserDataBlockTypes.Prefs].Should().Be("""{"ok":true}""");
        opened.Should().NotContainKey(UserDataBlockTypes.AppConfig);
        result.IsComplete.Should().BeFalse();
        result.FailedBlocks.Should().ContainSingle().Which.Should().Be(
            new UserDataBlockFailure(UserDataBlockTypes.AppConfig, UserDataBlockTypes.AppConfig, UserDataBlockFailureReason.AuthenticationFailed));
    }

    [Fact]
    public void OpenPack_SkipsMalformedBlock_KeepsGoodBlocks()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            keys.Kek,
            [
                new UserDataPlainBlock(UserDataBlockTypes.Prefs, UserDataBlockTypes.Prefs, """{"ok":true}"""),
                new UserDataPlainBlock(UserDataBlockTypes.AppConfig, UserDataBlockTypes.AppConfig, """{"cfg":1}"""),
            ]);

        UserDataBlockWire malformedBlock = Assert.IsType<UserDataBlockWire>(pack.Blocks[1]);
        malformedBlock.NonceBase64 = "not-base64";

        UserDataPackOpenResult result = UserDataPackCodec.OpenPack(pack, Username, keys.Kek);
        IReadOnlyDictionary<string, string> opened = result.Blocks;
        opened.Should().ContainKey(UserDataBlockTypes.Prefs);
        opened[UserDataBlockTypes.Prefs].Should().Be("""{"ok":true}""");
        opened.Should().NotContainKey(UserDataBlockTypes.AppConfig);
        result.IsComplete.Should().BeFalse();
        result.FailedBlocks.Should().ContainSingle().Which.Should().Be(
            new UserDataBlockFailure(UserDataBlockTypes.AppConfig, UserDataBlockTypes.AppConfig, UserDataBlockFailureReason.Malformed));
    }

    [Fact]
    public void OpenPack_DecodedNullEntry_ReportsMalformedAndKeepsGoodBlock()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            keys.Kek,
            [new UserDataPlainBlock(UserDataBlockTypes.Prefs, UserDataBlockTypes.Prefs, "{}")]);

        JsonObject json = EncodePackJson(pack);
        Assert.IsType<JsonArray>(json["blocks"]).Insert(0, null);

        UserDataPackOpenResult result = UserDataPackCodec.OpenPack(DecodePackJson(json), Username, keys.Kek);

        result.Blocks.Should().HaveCount(1).And.Contain(UserDataBlockTypes.Prefs, "{}");
        result.IsComplete.Should().BeFalse();
        result.FailedBlocks.Should().ContainSingle().Which.Should().Be(
            new UserDataBlockFailure(string.Empty, string.Empty, UserDataBlockFailureReason.Malformed));
    }

    [Theory]
    [InlineData("nonce_b64", false, null)]
    [InlineData("tag_b64", false, null)]
    [InlineData("ciphertext_b64", false, null)]
    [InlineData("id", false, null)]
    [InlineData("type", false, null)]
    [InlineData("id", true, null)]
    [InlineData("type", true, null)]
    [InlineData("id", false, "   ")]
    [InlineData("type", false, "   ")]
    public void OpenPack_DecodedMalformedBlockField_ReportsMalformedAndKeepsGoodBlock(string field, bool removeField, string? fieldValue)
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        const string badBlockId = "config";
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            keys.Kek,
            [
                new UserDataPlainBlock(badBlockId, UserDataBlockTypes.AppConfig, """{"cfg":1}"""),
                new UserDataPlainBlock(UserDataBlockTypes.Prefs, UserDataBlockTypes.Prefs, "{}"),
            ]);

        JsonObject json = EncodePackJson(pack);
        JsonObject badBlock = GetFirstBlockJson(json);
        if (removeField)
        {
            badBlock.Remove(field);
        }
        else
        {
            badBlock[field] = fieldValue;
        }

        UserDataPackOpenResult result = UserDataPackCodec.OpenPack(DecodePackJson(json), Username, keys.Kek);

        result.Blocks.Should().HaveCount(1).And.Contain(UserDataBlockTypes.Prefs, "{}");
        result.IsComplete.Should().BeFalse();
        result.FailedBlocks.Should().ContainSingle().Which.Should().Be(new UserDataBlockFailure(
            field == "id" ? string.Empty : badBlockId,
            field == "type" ? string.Empty : UserDataBlockTypes.AppConfig,
            UserDataBlockFailureReason.Malformed));
    }

    [Fact]
    public void OpenPack_DecodedUnknownTypeWithNullNonce_SkipsBlockByDefault()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = DecodePackWithUnknownTypeAndNullNonce(keys.Kek);

        UserDataPackOpenResult result = UserDataPackCodec.OpenPack(pack, Username, keys.Kek);

        result.Blocks.Should().HaveCount(1).And.Contain(UserDataBlockTypes.Prefs, "{}");
        result.IsComplete.Should().BeTrue();
        result.FailedBlocks.Should().BeEmpty();
    }

    [Fact]
    public void OpenPack_DecodedUnknownTypeWithNullNonce_SkippingDisabledReportsMalformedAndKeepsGoodBlock()
    {
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        UserDataPackWire pack = DecodePackWithUnknownTypeAndNullNonce(keys.Kek);

        UserDataPackOpenResult result = UserDataPackCodec.OpenPack(pack, Username, keys.Kek, skipUnknownTypes: false);

        result.Blocks.Should().HaveCount(1).And.Contain(UserDataBlockTypes.Prefs, "{}");
        result.IsComplete.Should().BeFalse();
        result.FailedBlocks.Should().ContainSingle().Which.Should().Be(
            new UserDataBlockFailure("extension", "future_extension", UserDataBlockFailureReason.Malformed));
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

        UserDataPackWire pack = new() { ContentVersion = 1, UsernameHashPrefix = "2bd806c9" };
        pack.Blocks.Add(block);
        string json = Encoding.UTF8.GetString(Convert.FromBase64String(UserDataPackCodec.EncodeBlob(pack)));
        json.Should().Be(
            """{"format":"cipherbank-userdata-pack-v1","content_version":1,"username_hash_prefix":"2bd806c9","blocks":[{"id":"prefs","type":"prefs","seq":0,"alg":"AES-256-GCM","nonce_b64":"AAAAAAAAAAAAAAAA","tag_b64":"VG\u002BE7OtAqIML1QgpsCaB\u002Bg==","ciphertext_b64":"MdzUOSWkOwN15\u002BhJB/NkSyMpq3pu"}]}""");

        UserDataPackCodec.OpenBlock(block, keys.Kek, hash, 1)
            .Should().Be("""{"APPEARANCE":"dark"}""");
    }

    private static UserDataPackWire DecodePackWithBlockField(string field, string? value, ReadOnlySpan<byte> kek)
    {
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            kek,
            [new UserDataPlainBlock("prefs", UserDataBlockTypes.Prefs, "{}")]);

        JsonObject json = EncodePackJson(pack);
        GetFirstBlockJson(json)[field] = value;
        return DecodePackJson(json);
    }

    private static UserDataPackWire DecodePackWithUnknownTypeAndNullNonce(ReadOnlySpan<byte> kek)
    {
        UserDataPackWire pack = UserDataPackCodec.SealPack(
            Username,
            contentVersion: 1,
            kek,
            [
                new UserDataPlainBlock("extension", "future_extension", "{}"),
                new UserDataPlainBlock(UserDataBlockTypes.Prefs, UserDataBlockTypes.Prefs, "{}"),
            ]);

        JsonObject json = EncodePackJson(pack);
        GetFirstBlockJson(json)["nonce_b64"] = null;
        return DecodePackJson(json);
    }

    private static JsonObject EncodePackJson(UserDataPackWire pack)
        => Assert.IsType<JsonObject>(JsonNode.Parse(Convert.FromBase64String(UserDataPackCodec.EncodeBlob(pack))));

    private static JsonObject GetFirstBlockJson(JsonObject pack)
        => Assert.IsType<JsonObject>(Assert.IsType<JsonArray>(pack["blocks"])[0]);

    private static UserDataPackWire DecodePackJson(JsonObject json)
        => UserDataPackCodec.DecodeBlob(Convert.ToBase64String(Encoding.UTF8.GetBytes(json.ToJsonString())));
}
