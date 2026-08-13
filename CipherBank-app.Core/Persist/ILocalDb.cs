// <copyright file="ILocalDb.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite public environment (Cora persist schema).</summary>
public interface ILocalDb
{
    string Path { get; }

    Task InitializeAsync();

    Task InitializeAsync(CancellationToken ct);

    ValueTask<CipherBankDbContext> CreateContextAsync();

    ValueTask<CipherBankDbContext> CreateContextAsync(CancellationToken ct);
}
