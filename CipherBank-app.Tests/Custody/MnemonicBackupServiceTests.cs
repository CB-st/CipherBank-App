// <copyright file="MnemonicBackupServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class MnemonicBackupServiceTests
{
    [Fact]
    public async Task RoundTrip_opens_same_mnemonic()
    {
        var svc = new MnemonicBackupService();
        string mnemonic = MnemonicHelper.Generate();

        byte[] file = await svc.CreateBackupFileAsync(mnemonic, "correct-horse-battery-staple");
        string opened = await svc.OpenBackupFileAsync(file, "correct-horse-battery-staple");

        opened.Should().Be(MnemonicHelper.Normalize(mnemonic));
        Encoding.UTF8.GetString(file).Should().NotContain(mnemonic.Split(' ')[0]);

        using JsonDocument json = JsonDocument.Parse(file);
        JsonElement root = json.RootElement;
        root.GetProperty("FORMAT").GetString().Should().Be("cipherbank-recovery-v1");
        root.GetProperty("KDF").GetString().Should().Be("PBKDF2-SHA256");
        root.GetProperty("ITERATIONS").GetInt32().Should().Be(600_000);
        root.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(
                "FORMAT",
                "KDF",
                "ITERATIONS",
                "SALT_B64",
                "NONCE_B64",
                "TAG_B64",
                "CIPHERTEXT_B64",
                "CREATED_AT");
    }

    [Fact]
    public async Task WrongPassword_throws()
    {
        var svc = new MnemonicBackupService();
        byte[] file = await svc.CreateBackupFileAsync(
            MnemonicHelper.Generate(),
            "correct-horse-battery-staple");

        Func<Task> act = async () => await svc.OpenBackupFileAsync(file, "wrong-password-here");

        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task ShortPassword_rejected_on_create()
    {
        var svc = new MnemonicBackupService();

        Func<Task> act = async () => await svc.CreateBackupFileAsync(MnemonicHelper.Generate(), "short");

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
