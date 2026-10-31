// <copyright file="MauiSecureStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;

namespace CipherBank_app.Services;

/// <summary>MAUI SecureStorage-backed ISecureStore.</summary>
public sealed class MauiSecureStore : ISecureStore
{
    public Task SetAsync(string key, string value)
        => SecureStorage.Default.SetAsync(key, value);

    public Task<string?> GetAsync(string key)
        => SecureStorage.Default.GetAsync(key);

    public Task RemoveAsync(string key)
    {
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }
}
