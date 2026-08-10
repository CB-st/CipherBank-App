// <copyright file="ProductSessionStorage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using CipherBank_app.V1;

namespace CipherBank_app.Services;

/// <summary>
/// Persists product /v1 session tokens in SecureStorage for the HTTP auth pipeline.
/// </summary>
public sealed class ProductSessionStorage : IProductSessionStore
{
    // --- SecureStorage keys (never log values) ---
    private const string AccessTokenKey = "product_access_token";
    private const string RefreshTokenKey = "product_refresh_token";
    private const string ExpiresUtcKey = "product_expires_utc";

    public async Task SaveAsync(SessionDto session)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, session.AccessToken).ConfigureAwait(false);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, session.RefreshToken).ConfigureAwait(false);
        DateTimeOffset expires = DateTimeOffset.FromUnixTimeMilliseconds(session.ExpiresAt);
        await SecureStorage.Default.SetAsync(ExpiresUtcKey, expires.ToString("O", CultureInfo.InvariantCulture)).ConfigureAwait(false);
    }

    public async Task<(string Access, string Refresh, DateTimeOffset Expires)?> GetAsync()
    {
        string? access = await SecureStorage.Default.GetAsync(AccessTokenKey).ConfigureAwait(false);
        string? refresh = await SecureStorage.Default.GetAsync(RefreshTokenKey).ConfigureAwait(false);
        string? expiresRaw = await SecureStorage.Default.GetAsync(ExpiresUtcKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh) || string.IsNullOrEmpty(expiresRaw))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(expiresRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset expires))
        {
            return null;
        }

        return (access, refresh, expires);
    }

    public void Clear()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(ExpiresUtcKey);
    }
}
