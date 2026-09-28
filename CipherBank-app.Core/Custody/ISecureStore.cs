// <copyright file="ISecureStore.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Platform-agnostic secure key/value store (SecureStorage / Keychain).</summary>
public interface ISecureStore
{
    /// <summary>
    /// Writes <paramref name="value"/> under <paramref name="key"/>, replacing any prior value.
    /// Use: Medium (custody / PIN writes). Scope: OS secure storage.
    /// </summary>
    Task SetAsync(string key, string value);

    /// <summary>
    /// Reads the value for <paramref name="key"/>, or null when absent.
    /// Use: High (custody / PIN reads). Scope: OS secure storage.
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Deletes <paramref name="key"/> when present; absent keys are a no-op.
    /// Use: Low (reset / staging cleanup). Scope: OS secure storage.
    /// </summary>
    Task RemoveAsync(string key);
}
