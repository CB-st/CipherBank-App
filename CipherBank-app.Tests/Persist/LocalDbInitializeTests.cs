// <copyright file="LocalDbInitializeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class LocalDbInitializeTests
{
    /// <summary>
    /// Fresh initialize creates the EF model tables as empty sets.
    /// Use: Medium (Persist gate). Scope: LocalDb.InitializeAsync.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_CreatesEmptyModelTables()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-init-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        db.Path.Should().Be(Path.GetFullPath(path));

        await using CipherBankDbContext context = await db.CreateContextAsync();
        (await context.Wallets.CountAsync()).Should().Be(0);
        (await context.Recipients.CountAsync()).Should().Be(0);
        (await context.Preferences.CountAsync()).Should().Be(0);
        (await context.RateSnapshots.CountAsync()).Should().Be(0);
        (await context.OhlcPoints.CountAsync()).Should().Be(0);
        (await context.SyncMetadata.CountAsync()).Should().Be(0);
    }
}
