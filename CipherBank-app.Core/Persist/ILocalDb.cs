// <copyright file="ILocalDb.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite public environment (Cora persist schema).</summary>
public interface ILocalDb
{
    /// <summary>Gets the on-disk SQLite path for this database instance (SQLite DataSource).</summary>
    string Path { get; }

    /// <summary>
    /// Applies pending EF migrations on first open without caller cancellation.
    /// Use: High (app start / first persist call). Scope: ILocalDb consumers.
    /// </summary>
    Task InitializeAsync() => InitializeAsync(CancellationToken.None);

    /// <summary>Applies pending EF migrations on first open, honoring <paramref name="ct"/>.</summary>
    Task InitializeAsync(CancellationToken ct);

    /// <summary>
    /// Opens an EF context without caller cancellation. The caller owns and disposes it.
    /// Use: High (every repository call). Scope: ILocalDb consumers.
    /// </summary>
    ValueTask<CipherBankDbContext> CreateContextAsync()
        => CreateContextAsync(CancellationToken.None);

    /// <summary>Opens an EF context, honoring <paramref name="ct"/>; the caller owns and disposes it.</summary>
    ValueTask<CipherBankDbContext> CreateContextAsync(CancellationToken ct);
}
