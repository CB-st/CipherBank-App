// <copyright file="InMemoryProductSessionStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>In-memory product session store (tests / mock DI).</summary>
public sealed class InMemoryProductSessionStore : IProductSessionStore
{
    private SessionDto? _session;

    public Task SaveAsync(SessionDto session)
    {
        _session = session;
        return Task.CompletedTask;
    }

    public Task<(string Access, string Refresh, DateTimeOffset Expires)?> GetAsync()
    {
        if (_session is null || string.IsNullOrEmpty(_session.AccessToken))
        {
            return Task.FromResult<(string, string, DateTimeOffset)?>(null);
        }

        return Task.FromResult<(string, string, DateTimeOffset)?>(
            (_session.AccessToken, _session.RefreshToken, DateTimeOffset.FromUnixTimeMilliseconds(_session.ExpiresAt)));
    }

    public void Clear() => _session = null;
}
