// <copyright file="LocalWalletRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist.Entities;

namespace CipherBank_app.Persist;

/// <summary>Local wallet index row.</summary>
public sealed record LocalWalletRow(
    string Id,
    string Symbol,
    string? Label,
    string? Address,
    string? Path,
    int AccountIndex,
    string Kind,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalWalletRow"/> class from a persisted
    /// entity. Use: High (every wallet list read). Scope: WalletRepository projections.
    /// </summary>
    public LocalWalletRow(WalletEntity entity)
        : this(
            entity.Id,
            entity.Symbol,
            entity.Label,
            entity.Address,
            entity.Path,
            entity.AccountIndex,
            entity.Kind,
            entity.CreatedAt)
    {
    }
}
