// <copyright file="IRecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite ACH recipients repo (Cora recipientsRepo).</summary>
public interface IRecipientRepository
{
    Task EnsureSchemaAsync();

    Task<IReadOnlyList<AchRecipientRow>> ListAsync();

    Task UpsertAsync(AchRecipientRow row);

    Task DeleteAsync(string id);

    Task SeedDefaultsIfEmptyAsync();
}
