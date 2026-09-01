// <copyright file="LocalDbInitializeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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
        LocalDb db = new LocalDb(new FileInfo(path));
        await db.InitializeAsync();
        db.Path.Should().Be(Path.GetFullPath(path));

        CipherBankDbContext context = await db.CreateContextAsync();
        await using (context)
        {
            (await context.Wallets.CountAsync()).Should().Be(0);
            (await context.Recipients.CountAsync()).Should().Be(0);
            (await context.Preferences.CountAsync()).Should().Be(0);
            (await context.RateSnapshots.CountAsync()).Should().Be(0);
            (await context.OhlcPoints.CountAsync()).Should().Be(0);
            (await context.SyncMetadata.CountAsync()).Should().Be(0);
        }
    }
}
