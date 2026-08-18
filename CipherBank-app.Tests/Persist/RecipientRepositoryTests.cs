// <copyright file="RecipientRepositoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class RecipientRepositoryTests
{
    [Fact]
    public async Task SeedAndList_Works()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-test-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        RecipientRepository repo = new RecipientRepository(db);
        await repo.SeedDefaultsIfEmptyAsync();
        IReadOnlyList<AchRecipientRow> list = await repo.ListAsync();
        list.Should().HaveCountGreaterThanOrEqualTo(2);
        await repo.SeedDefaultsIfEmptyAsync();
        (await repo.ListAsync()).Should().HaveCount(list.Count);
    }

    /// <summary>
    /// Concurrent first-run seeds must produce one default set with stable IDs.
    /// Use: Medium (review regression). Scope: RecipientRepositoryTests.
    /// </summary>
    [Fact]
    public async Task SeedDefaultsIfEmptyAsync_ConcurrentCallsCreateOneDefaultSet()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-test-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        RecipientRepository repo = new RecipientRepository(db);

        await Task.WhenAll(repo.SeedDefaultsIfEmptyAsync(), repo.SeedDefaultsIfEmptyAsync());

        IReadOnlyList<AchRecipientRow> listed = await repo.ListAsync();
        listed.Should().HaveCount(2);
        listed.Select(row => row.Id).Should().BeEquivalentTo(
        [
            RecipientRepository.DefaultRentRecipientId,
            RecipientRepository.DefaultUtilitiesRecipientId,
        ]);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyRecipientWithMatchingId()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-test-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        RecipientRepository repo = new RecipientRepository(db);
        AchRecipientRow recipientToDelete = new AchRecipientRow(
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
        AchRecipientRow recipientToKeep = recipientToDelete with { Id = "keep-me", Name = "Keep me" };
        await repo.UpsertAsync(recipientToDelete);
        await repo.UpsertAsync(recipientToKeep);

        await repo.DeleteAsync(recipientToDelete.Id);

        (await repo.ListAsync()).Should().ContainSingle().Which.Id.Should().Be(recipientToKeep.Id);
    }

    [Fact]
    public async Task UpsertAsync_DoesNotPersistCleartextAccountOrRouting()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-test-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        RecipientRepository repo = new RecipientRepository(db);
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

        IReadOnlyList<AchRecipientRow> listed = await repo.ListAsync();
        listed.Should().ContainSingle();
        listed[0].Account.Should().BeNull();
        listed[0].Routing.Should().BeNull();
        listed[0].AccountMask.Should().Be(AchRecipientValidation.MaskAccount("88210001"));
        listed[0].RoutingMask.Should().Be(AchRecipientValidation.MaskRouting("021000021"));

        await using CipherBankDbContext context = await db.CreateContextAsync();
        SqliteConnection conn = (SqliteConnection)context.Database.GetDbConnection();
        await conn.OpenAsync();
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT account_mask, routing_mask FROM recipients WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", "payee-1");
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetString(0).Should().Contain("0001");
        reader.GetString(1).Should().Contain("0021");
        await reader.DisposeAsync();

        await using SqliteCommand schema = conn.CreateCommand();
        schema.CommandText = "SELECT name FROM pragma_table_info('recipients')";
        List<string> columns = new List<string>();
        await using SqliteDataReader schemaReader = await schema.ExecuteReaderAsync();
        while (await schemaReader.ReadAsync())
        {
            columns.Add(schemaReader.GetString(0));
        }

        columns.Should().NotContain("account");
        columns.Should().NotContain("routing");
        columns.Should().NotContain("account_full");
    }

    /// <summary>
    /// Replacing cleartext on an already-masked row must refresh stored masks.
    /// Use: Medium (review regression). Scope: RecipientRepositoryTests.
    /// </summary>
    [Fact]
    public async Task UpsertAsync_RecomputesMasksWhenCleartextReplaced()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-test-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        RecipientRepository repo = new RecipientRepository(db);
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

        AchRecipientRow listed = (await repo.ListAsync()).Should().ContainSingle().Subject;
        await repo.UpsertAsync(listed with
        {
            Account = "99998888",
            Routing = "021000021",
        });

        AchRecipientRow updated = (await repo.ListAsync()).Should().ContainSingle().Subject;
        updated.AccountMask.Should().Be(AchRecipientValidation.MaskAccount("99998888"));
        updated.RoutingMask.Should().Be(AchRecipientValidation.MaskRouting("021000021"));
    }
}
