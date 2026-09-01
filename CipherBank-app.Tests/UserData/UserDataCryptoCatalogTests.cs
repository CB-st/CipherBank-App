// <copyright file="UserDataCryptoCatalogTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.UserData;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class UserDataCryptoCatalogTests
{
    private const string FixtureMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Fact]
    public void DefaultCatalog_UsesRsaAesGcmSuite()
    {
        UserDataCryptoCatalog catalog = new UserDataCryptoCatalog();
        catalog.ActiveSuiteId.Should().Be(UserDataConstants.SuiteRsaAesGcmV1);
        catalog.Active.Enroll.AlgorithmId.Should().Be(UserDataConstants.EnrollAlgorithmRsaOaepSha256V1);
        catalog.Active.Blocks.AlgorithmId.Should().Be(UserDataConstants.SymmetricAlgorithmAesGcmV1);
        catalog.Active.Symmetric.AlgorithmId.Should().Be(UserDataConstants.SymmetricAlgorithmAesGcmV1);
    }

    [Fact]
    public void Suite_SealOpenPack_ViaCatalogBlocks()
    {
        UserDataCryptoCatalog catalog = new UserDataCryptoCatalog();
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);

        UserDataPackWire pack = UserDataPackCodec.SealPack(
            "alice",
            contentVersion: 2,
            keys.Kek,
            [new UserDataPlainBlock("prefs", UserDataBlockTypes.Prefs, """{"ok":true}""")],
            catalog.Active.Blocks);

        Dictionary<string, string> opened = UserDataPackCodec.OpenPack(
            pack,
            "alice",
            keys.Kek,
            catalog.Active.Blocks);

        opened["prefs"].Should().Be("""{"ok":true}""");
    }

    [Fact]
    public void Symmetric_InternalSealOpen_RoundTrips()
    {
        UserDataCryptoCatalog catalog = new UserDataCryptoCatalog();
        using UserDataKeyMaterial keys = UserDataKeyDerivation.Derive(FixtureMnemonic);
        byte[] plain = "internal-secret"u8.ToArray();
        byte[] aad = "aad"u8.ToArray();

        UserDataSymmetricBlob blob = catalog.Active.Symmetric.Seal(plain, keys.Kek, aad);
        byte[] opened = catalog.Active.Symmetric.Open(blob, keys.Kek, aad);
        opened.Should().Equal(plain);
    }

    [Fact]
    public void SetActive_UnknownSuite_Throws()
    {
        UserDataCryptoCatalog catalog = new UserDataCryptoCatalog();
        Action act = () => catalog.SetActive(UserDataConstants.SuitePqAesGcmV1);
        act.Should().Throw<ArgumentException>();
    }
}
