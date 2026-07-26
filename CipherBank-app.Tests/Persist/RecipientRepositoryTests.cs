// <copyright file="RecipientRepositoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class RecipientRepositoryTests
{
    [Fact]
    public async Task SeedAndList_Works()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-test-" + Guid.NewGuid().ToString("N") + ".db");
        var db = new LocalDb(path);
        await db.InitializeAsync();
        var repo = new RecipientRepository(db);
        await repo.SeedDefaultsIfEmptyAsync();
        var list = await repo.ListAsync();
        list.Should().HaveCountGreaterThanOrEqualTo(2);
        await repo.SeedDefaultsIfEmptyAsync();
        (await repo.ListAsync()).Should().HaveCount(list.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyRecipientWithMatchingId()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-test-" + Guid.NewGuid().ToString("N") + ".db");
        var db = new LocalDb(path);
        await db.InitializeAsync();
        var repo = new RecipientRepository(db);
        var recipientToDelete = new AchRecipientRow(
            "delete-me",
            "Delete me",
            null,
            null,
            null,
            null,
            "checking",
            null,
            null,
            null,
            DateTimeOffset.UtcNow);
        var recipientToKeep = recipientToDelete with { Id = "keep-me", Name = "Keep me" };
        await repo.UpsertAsync(recipientToDelete);
        await repo.UpsertAsync(recipientToKeep);

        await repo.DeleteAsync(recipientToDelete.Id);

        (await repo.ListAsync()).Should().ContainSingle().Which.Id.Should().Be(recipientToKeep.Id);
    }

    [Fact]
    public async Task UpsertAsync_DoesNotPersistCleartextAccountOrRouting()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-test-" + Guid.NewGuid().ToString("N") + ".db");
        var db = new LocalDb(path);
        await db.InitializeAsync();
        var repo = new RecipientRepository(db);
        await repo.UpsertAsync(new AchRecipientRow(
            "payee-1",
            "Payee",
            "Holder",
            "Bank",
            "021000021",
            "88210001",
            "checking",
            null,
            null,
            null,
            DateTimeOffset.UtcNow));

        var listed = await repo.ListAsync();
        listed.Should().ContainSingle();
        listed[0].Account.Should().BeNull();
        listed[0].Routing.Should().BeNull();
        listed[0].AccountMask.Should().Be(AchRecipientValidation.MaskAccount("88210001"));
        listed[0].RoutingMask.Should().Be(AchRecipientValidation.MaskRouting("021000021"));

        await using var conn = db.Open();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT account, routing, account_mask, routing_mask FROM recipients WHERE id='payee-1'";
        await using var reader = await cmd.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.IsDBNull(0).Should().BeTrue();
        reader.IsDBNull(1).Should().BeTrue();
        reader.GetString(2).Should().Contain("0001");
        reader.GetString(3).Should().Contain("0021");
    }
}
