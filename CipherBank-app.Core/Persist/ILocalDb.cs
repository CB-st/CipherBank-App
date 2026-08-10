// <copyright file="ILocalDb.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite public environment (Cora persist schema).</summary>
public interface ILocalDb
{
    string Path { get; }

    Task InitializeAsync(CancellationToken ct = default);

    ValueTask<CipherBankDbContext> CreateContextAsync(CancellationToken ct = default);
}
