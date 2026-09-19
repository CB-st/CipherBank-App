// <copyright file="IWalletRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>
/// SQLite wallets repo, composed from cancelable role seams. Port invariants: rows carry
/// address/path metadata only — key material never enters this store — and lists return
/// wallets ordered by creation time.
/// </summary>
public interface IWalletRepository
    : IListable<LocalWalletDescriptor>, IUpsert<LocalWalletDescriptor>, IDeleteById;
