// <copyright file="MauiAppClipboard.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>MAUI <see cref="Clipboard"/> adapter.</summary>
public sealed class MauiAppClipboard : IAppClipboard
{
    public Task SetTextAsync(string text) => Clipboard.Default.SetTextAsync(text);
}
