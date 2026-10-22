// <copyright file="IRecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// SQLite ACH recipients repo (Cora recipientsRepo), composed from cancelable role seams.
/// Port invariants: rows are mask-only (cleartext account/routing inputs never enter the
/// EF model), lists return payees in stored order for the picker, and the schema is
/// ensured before the first payee read or write.
/// </summary>
public interface IRecipientRepository
    : IListable<AchRecipientRow>, IUpsert<AchRecipientRow>, IDeleteById;
