// <copyright file="IUserDataPackMetaStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Persists pack content_version and dual-write success counts.</summary>
public interface IUserDataPackMetaStore
{
    /// <summary>Loads meta (defaults to zeros). Use: High (sync). Scope: userdata prefs sync.</summary>
    Task<UserDataPackMeta> LoadAsync(CancellationToken ct);

    /// <summary>Saves meta. Use: High (sync). Scope: userdata prefs sync.</summary>
    Task SaveAsync(UserDataPackMeta meta, CancellationToken ct);
}
