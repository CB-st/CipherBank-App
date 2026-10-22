// <copyright file="AchRecipientRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Persist.Entities;

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
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AchRecipientRow"/> class from a configured
    /// seed. Masks stay null: the entity writer computes them from the cleartext inputs.
    /// Use: Low (startup seeding). Scope: RecipientSeedInitializer.
    /// </summary>
    public AchRecipientRow(DefaultRecipientOptions seed, DateTimeOffset createdAt)
        : this(
            seed.Id,
            seed.Name,
            seed.Holder,
            seed.Bank,
            seed.Routing,
            seed.Account,
            seed.AccountType,
            seed.Memo,
            null,
            null,
            createdAt)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AchRecipientRow"/> class from a persisted
    /// entity. Routing and Account stay null: the store holds masks only, never cleartext.
    /// Use: High (every recipient list read). Scope: RecipientRepository projections.
    /// </summary>
    public AchRecipientRow(RecipientEntity entity)
        : this(
            entity.Id,
            entity.Name,
            entity.Holder,
            entity.Bank,
            null,
            null,
            entity.AccountType,
            entity.Memo,
            entity.AccountMask,
            entity.RoutingMask,
            entity.CreatedAt)
    {
    }
}
