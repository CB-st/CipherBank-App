// <copyright file="CipherBankDbContextFactoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class CipherBankDbContextFactoryTests
{
    /// <summary>
    /// Design-time factory returns a SQLite context the EF tools can migrate.
    /// Use: Low. Scope: CipherBankDbContextFactory.
    /// </summary>
    [Fact]
    public void CreateDbContext_ReturnsSqliteContext()
    {
        CipherBankDbContextFactory factory = new CipherBankDbContextFactory();
        using CipherBankDbContext context = factory.CreateDbContext([]);
        context.Database.IsSqlite().Should().BeTrue();
        context.Database.GetDbConnection().DataSource.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Design-time factory ignores args; <c>dotnet ef</c> may pass an empty array.
    /// Use: Low. Scope: CipherBankDbContextFactory.
    /// </summary>
    [Fact]
    public void CreateDbContext_NullArgs_AreIgnored()
    {
        CipherBankDbContextFactory factory = new CipherBankDbContextFactory();
        using CipherBankDbContext context = factory.CreateDbContext(null!);
        context.Database.IsSqlite().Should().BeTrue();
    }
}
