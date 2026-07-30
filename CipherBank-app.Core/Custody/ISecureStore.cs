// <copyright file="ISecureStore.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Platform-agnostic secure key/value store (SecureStorage / Keychain).</summary>
public interface ISecureStore
{
    Task SetAsync(string key, string value);

    Task<string?> GetAsync(string key);

    Task RemoveAsync(string key);
}
