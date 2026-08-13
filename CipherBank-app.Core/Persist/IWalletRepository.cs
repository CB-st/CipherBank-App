// <copyright file="IWalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>SQLite wallets repo.</summary>
public interface IWalletRepository
{
    Task<IReadOnlyList<LocalWalletRow>> ListAsync();

    Task UpsertAsync(LocalWalletRow row);

    Task DeleteAsync(string id);
}
