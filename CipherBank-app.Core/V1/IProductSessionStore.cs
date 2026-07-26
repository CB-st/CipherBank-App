// <copyright file="IProductSessionStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Persists product /v1 session tokens (access + refresh).</summary>
public interface IProductSessionStore
{
    Task SaveAsync(SessionDto session);

    Task<(string Access, string Refresh, DateTimeOffset Expires)?> GetAsync();

    void Clear();
}
