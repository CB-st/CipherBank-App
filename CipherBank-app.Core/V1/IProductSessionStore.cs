// <copyright file="IProductSessionStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Persists product /v1 session tokens (access + refresh).</summary>
public interface IProductSessionStore
{
    /// <summary>
    /// Persists the session tokens, replacing any prior record.
    /// Use: Medium (session create / refresh). Scope: this device's token store.
    /// </summary>
    Task SaveAsync(SessionDto session);

    /// <summary>
    /// Returns the stored tokens, or null when no session is persisted.
    /// Use: High (every authenticated request). Scope: this device's token store.
    /// </summary>
    Task<(string Access, string Refresh, DateTimeOffset Expires)?> GetAsync();

    /// <summary>
    /// Deletes the stored tokens (lock / logout).
    /// Use: High (lock). Scope: this device's token store.
    /// </summary>
    void Clear();
}
