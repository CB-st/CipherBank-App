// <copyright file="MnemonicBackupServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
        MnemonicBackupService svc = new MnemonicBackupService();
        string mnemonic = MnemonicHelper.Generate();

        byte[] file = await svc.CreateBackupFileAsync(mnemonic, "correct-horse-battery-staple", default);
        string opened = await svc.OpenBackupFileAsync(file, "correct-horse-battery-staple", default);

        opened.Should().Be(MnemonicHelper.Normalize(mnemonic));
        Encoding.UTF8.GetString(file).Should().NotContain(MnemonicHelper.Normalize(mnemonic));

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
        MnemonicBackupService svc = new MnemonicBackupService();
        byte[] file = await svc.CreateBackupFileAsync(
            MnemonicHelper.Generate(),
            "correct-horse-battery-staple",
            default);

        Func<Task> act = async () => await svc.OpenBackupFileAsync(file, "wrong-password-here", default);

        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task ShortPassword_rejected_on_create()
    {
        MnemonicBackupService svc = new MnemonicBackupService();

        Func<Task> act = async () => await svc.CreateBackupFileAsync(MnemonicHelper.Generate(), "short", default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_minimum_created_at_rejected(bool includeMinimumCreatedAt)
    {
        MnemonicBackupService svc = new MnemonicBackupService();
        byte[] validFile = await svc.CreateBackupFileAsync(
            MnemonicHelper.Generate(),
            "correct-horse-battery-staple",
            default);
        using JsonDocument validJson = JsonDocument.Parse(validFile);
        Dictionary<string, JsonElement> fields = validJson.RootElement.EnumerateObject()
            .Where(property => property.Name != "CREATED_AT")
            .ToDictionary(property => property.Name, property => property.Value.Clone());

        if (includeMinimumCreatedAt)
        {
            fields["CREATED_AT"] = JsonSerializer.SerializeToElement(DateTimeOffset.MinValue);
        }

        byte[] invalidFile = JsonSerializer.SerializeToUtf8Bytes(fields);
        Func<Task> act = async () =>
            await svc.OpenBackupFileAsync(invalidFile, "correct-horse-battery-staple", default);

        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Theory]
    [InlineData("""{"FORMAT":""")]
    [InlineData("""{"FORMAT":"cipherbank-recovery-v1","KDF":"PBKDF2-SHA256","ITERATIONS":600000,"SALT_B64":"***","NONCE_B64":"AAAAAAAAAAAAAAAA","TAG_B64":"AAAAAAAAAAAAAAAAAAAAAA==","CIPHERTEXT_B64":"AA==","CREATED_AT":"2026-07-20T00:00:00+00:00"}""")]
    public async Task Malformed_recovery_file_throws_cryptographic_exception(string invalidJson)
    {
        MnemonicBackupService svc = new MnemonicBackupService();

        Func<Task> act = async () => await svc.OpenBackupFileAsync(
            Encoding.UTF8.GetBytes(invalidJson),
            "correct-horse-battery-staple",
            default);

        await act.Should().ThrowAsync<CryptographicException>();
    }
}
