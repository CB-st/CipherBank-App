// <copyright file="IAppClipboard.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Platform clipboard adapter for ViewModels (testable; no MAUI statics).</summary>
public interface IAppClipboard
{
    /// <summary>Copies <paramref name="text"/> to the OS clipboard, replacing prior contents.</summary>
    Task SetTextAsync(string text);
}
