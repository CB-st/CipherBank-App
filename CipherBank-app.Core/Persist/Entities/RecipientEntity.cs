// <copyright file="RecipientEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record RecipientEntity
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
