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
}
