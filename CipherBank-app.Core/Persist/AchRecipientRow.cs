// <copyright file="AchRecipientRow.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>ACH / payee recipient stored on device.</summary>
/// <remarks>
/// Full account/routing digits are accepted on upsert only to compute masks; SQLite (the public
/// environment) persists masks and metadata — never cleartext PAN/routing.
/// </remarks>
public sealed record AchRecipientRow(
    string Id,
    string Name,
    string? Holder,
    string? Bank,
    string? Routing,
    string? Account,
    string AccountType,
    string? Memo,
    string? AccountMask,
    string? RoutingMask,
    DateTimeOffset CreatedAt);
