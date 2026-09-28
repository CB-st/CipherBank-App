// <copyright file="IOptionsSection.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>
/// Convention contract for options classes bound from repository configuration: the type
/// itself names its configuration section, so binders resolve the key from the class
/// (<c>T.SectionName</c>) instead of repeating string literals at call sites.
/// </summary>
public interface IOptionsSection
{
    /// <summary>
    /// Gets the configuration section name this options class binds from.
    /// Use: Medium (host/test binding). Scope: options composition.
    /// </summary>
    static abstract string SectionName { get; }
}
