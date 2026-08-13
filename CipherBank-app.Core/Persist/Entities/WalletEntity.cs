// <copyright file="WalletEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record WalletEntity
{
    public string Id { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public string? Label { get; set; }

    public string? Address { get; set; }

    public string? Path { get; set; }

    public int AccountIndex { get; set; }

    public string Kind { get; set; } = "derived";

    public DateTimeOffset CreatedAt { get; set; }
}
