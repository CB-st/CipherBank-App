// <copyright file="CipherBankDbContextFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CipherBank_app.Persist;

/// <summary>Design-time factory so <c>dotnet ef</c> can add migrations against Core.</summary>
public sealed class CipherBankDbContextFactory : IDesignTimeDbContextFactory<CipherBankDbContext>
{
    /// <summary>Builds a temporary SQLite context for <c>dotnet ef migrations add</c>.</summary>
    public CipherBankDbContext CreateDbContext(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cipherbank-design.db");
        string connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        DbContextOptionsBuilder<CipherBankDbContext> builder = new();
        builder.UseSqlite(connectionString);
        return new CipherBankDbContext(builder.Options);
    }
}
