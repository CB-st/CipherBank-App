// <copyright file="WalletEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist.Entities;

public sealed record WalletEntity
{
    public string Id { get; init; } = string.Empty;

    public string Symbol { get; init; } = string.Empty;

    public string? Label { get; init; }

    public string? Address { get; init; }

    public string? Path { get; init; }

    public int AccountIndex { get; init; }

    public string Kind { get; init; } = "derived";

    public DateTimeOffset CreatedAt { get; init; }

    internal LocalWalletDescriptor ToDescriptor() =>
        new(
            Id,
            AssetSymbol.Parse(Symbol),
            Label,
            Address,
            Path,
            AccountIndex,
            Kind,
            CreatedAt);
}
