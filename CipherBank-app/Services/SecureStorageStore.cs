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
        if (key.Length == 0)
        {
            throw new ArgumentException("Key must not be empty.", nameof(key));
        }

        return SecureStorage.Default.SetAsync(key, value);
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key)
    {
        if (key.Length == 0)
        {
            throw new ArgumentException("Key must not be empty.", nameof(key));
        }

        return SecureStorage.Default.GetAsync(key);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        if (key.Length == 0)
        {
            throw new ArgumentException("Key must not be empty.", nameof(key));
        }

        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}
