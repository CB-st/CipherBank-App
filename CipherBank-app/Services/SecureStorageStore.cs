// <copyright file="SecureStorageStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;

namespace CipherBank_app.Services;

/// <summary>Stores custody secrets in the platform secure store.</summary>
public sealed class SecureStorageStore : ISecureStore
{
    /// <inheritdoc />
    public Task SetAsync(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        return SecureStorage.Default.SetAsync(key, value);
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return SecureStorage.Default.GetAsync(key);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}
