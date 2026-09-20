// <copyright file="IRecipientSeedInitializer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Initializes optional environment-owned recipient seed data.</summary>
public interface IRecipientSeedInitializer
{
    /// <summary>
    /// Inserts the configured recipient set only when the table is empty.
    /// Use: High (first payee list). Scope: application database.
    /// </summary>
    Task InitializeAsync() => InitializeAsync(CancellationToken.None);

    Task InitializeAsync(CancellationToken ct);
}
