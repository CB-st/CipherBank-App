// <copyright file="ILocalDb.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite public environment (Cora persist schema).</summary>
public interface ILocalDb
{
    /// <summary>Gets the on-disk SQLite path for this database instance.</summary>
    string Path { get; }

    /// <summary>
    /// Applies pending EF migrations on first open.
    /// Use: High (app start / first persist call). Scope: ILocalDb consumers.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Applies pending EF migrations on first open, honoring <paramref name="ct"/>.
    /// Use: High (app start / first persist call). Scope: ILocalDb consumers.
    /// </summary>
    Task InitializeAsync(CancellationToken ct);

    /// <summary>
    /// Opens an EF context. The caller owns the returned value and must dispose it.
    /// Use: High (every repository call). Scope: ILocalDb consumers.
    /// </summary>
    ValueTask<CipherBankDbContext> CreateContextAsync();

    /// <summary>
    /// Opens an EF context, honoring <paramref name="ct"/>. The caller owns the returned value and must dispose it.
    /// Use: High (every repository call). Scope: ILocalDb consumers.
    /// </summary>
    ValueTask<CipherBankDbContext> CreateContextAsync(CancellationToken ct);
}
