// <copyright file="InMemoryUserDataPackMetaStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Process-local pack meta (tests / early wiring before SQLite meta key).</summary>
public sealed class InMemoryUserDataPackMetaStore : IUserDataPackMetaStore
{
    private readonly object _gate = new();
    private UserDataPackMeta _meta = new();

    /// <inheritdoc />
    public Task<UserDataPackMeta> LoadAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult(new UserDataPackMeta
            {
                ContentVersion = _meta.ContentVersion,
                SuccessfulPackWrites = _meta.SuccessfulPackWrites,
            });
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(UserDataPackMeta meta, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(meta);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _meta = new UserDataPackMeta
            {
                ContentVersion = meta.ContentVersion,
                SuccessfulPackWrites = meta.SuccessfulPackWrites,
            };
        }

        return Task.CompletedTask;
    }
}
