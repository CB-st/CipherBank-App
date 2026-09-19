// <copyright file="ICoraLineProvider.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Cora;

/// <summary>Resolves localizable Cora copy for a screen key.</summary>
public interface ICoraLineProvider
{
    /// <summary>
    /// Returns the Cora line for <paramref name="screen"/> (e.g. <c>home</c>, <c>convert</c>),
    /// falling back to the configured default when the key is unknown.
    /// </summary>
    string GetLine(string screen);
}
