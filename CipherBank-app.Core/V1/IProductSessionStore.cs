// <copyright file="IProductSessionStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Persists product /v1 session tokens (access + refresh).</summary>
public interface IProductSessionStore
{
    Task SaveAsync(SessionDto session);

    Task<(string Access, string Refresh, DateTimeOffset Expires)?> GetAsync();

    void Clear();
}
