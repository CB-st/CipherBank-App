// <copyright file="IWalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite wallets repo.</summary>
public interface IWalletRepository
{
    Task<IReadOnlyList<LocalWalletRow>> ListAsync();

    Task UpsertAsync(LocalWalletRow row);

    Task DeleteAsync(string id);
}
