// <copyright file="RecipientSeedInitializerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.Tests.Configuration;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class RecipientSeedInitializerTests
{
    [Fact]
    public async Task InitializeAsync_UsesConfiguredStableIds()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-seed-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new(new FileInfo(path));
        RecipientSeedInitializer initializer = new(
            db,
            EmbeddedAppSettings.BindPersistence("Development"),
            TimeProvider.System);

        await initializer.InitializeAsync(default);

        RecipientRepository repository = new(db);
        IReadOnlyList<AchRecipientRow> rows = await repository.ListAsync();
        rows.Select(row => row.Id).Should().BeEquivalentTo(
            "seed:rent-4th-st",
            "seed:utilities-co");
    }

    [Fact]
    public async Task InitializeAsync_SeparateConnections_CreateOneConfiguredSet()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-seed-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb firstDb = new(new FileInfo(path));
        LocalDb secondDb = new(new FileInfo(path));
        RecipientSeedInitializer first = new(
            firstDb,
            EmbeddedAppSettings.BindPersistence("Development"),
            TimeProvider.System);
        RecipientSeedInitializer second = new(
            secondDb,
            EmbeddedAppSettings.BindPersistence("Development"),
            TimeProvider.System);

        await Task.WhenAll(first.InitializeAsync(default), second.InitializeAsync(default));

        RecipientRepository repository = new(firstDb);
        IReadOnlyList<AchRecipientRow> rows = await repository.ListAsync();
        rows.Should().HaveCount(2);
        rows.Select(row => row.Id).Should().OnlyHaveUniqueItems();
    }
}
