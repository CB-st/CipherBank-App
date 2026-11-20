// <copyright file="RecipientEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record RecipientEntity
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Holder { get; init; }

    public string? Bank { get; init; }

    public string AccountType { get; init; } = "checking";

    public string? Memo { get; init; }

    public string? AccountMask { get; init; }

    public string? RoutingMask { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
