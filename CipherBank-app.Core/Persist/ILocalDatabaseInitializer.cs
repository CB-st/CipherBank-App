// <copyright file="ILocalDatabaseInitializer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Performs one-time local database cleanup and migration.</summary>
public interface ILocalDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
