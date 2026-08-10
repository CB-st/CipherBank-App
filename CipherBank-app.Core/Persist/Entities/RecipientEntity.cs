// <copyright file="RecipientEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed class RecipientEntity
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Holder { get; set; }

    public string? Bank { get; set; }

    public string AccountType { get; set; } = "checking";

    public string? Memo { get; set; }

    public string? AccountMask { get; set; }

    public string? RoutingMask { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
