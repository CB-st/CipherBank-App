// <copyright file="AppStartupCoordinator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Orders persistence initialization before recipient seeding.</summary>
public sealed class AppStartupCoordinator(
    ILocalDatabaseInitializer database,
    IRecipientSeedInitializer recipients)
{
    private readonly Lock _gate = new();
    private Task? _startup;

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
        await database.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await recipients.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
