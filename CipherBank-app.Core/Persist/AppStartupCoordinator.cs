// <copyright file="AppStartupCoordinator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Orders persistence initialization before recipient seeding.</summary>
public sealed class AppStartupCoordinator
{
    private readonly Lock _gate = new();
    private readonly ILocalDatabaseInitializer _database;
    private readonly IRecipientSeedInitializer _recipients;
    private Task? _startup;

    public AppStartupCoordinator(
        ILocalDatabaseInitializer database,
        IRecipientSeedInitializer recipients)
    {
        _database = database;
        _recipients = recipients;
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_startup is null || _startup.IsFaulted || _startup.IsCanceled)
            {
                _startup = InitializeCoreAsync(cancellationToken);
            }

            return _startup;
        }
    }

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        await _database.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _recipients.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
