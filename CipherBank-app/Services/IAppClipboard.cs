// <copyright file="IAppClipboard.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Platform clipboard adapter for ViewModels (testable; no MAUI statics).</summary>
public interface IAppClipboard
{
    Task SetTextAsync(string text);
}
