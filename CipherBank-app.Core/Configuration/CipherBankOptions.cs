// <copyright file="CipherBankOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>
/// Class-named options parent. <see cref="SectionName"/> is <c>typeof(TSelf).Name</c>,
/// which is <c>nameof</c> of the concrete options class.
/// </summary>
/// <typeparam name="TSelf">The concrete options type.</typeparam>
public abstract record CipherBankOptions<TSelf>
    where TSelf : CipherBankOptions<TSelf>
{
    /// <summary>
    /// Configuration section key for <typeparamref name="TSelf"/>.
    /// Registration still binds <c>GetType().Name</c> so a subclass cannot publish a different key.
    /// </summary>
    public string SectionName => typeof(TSelf).Name;
}
