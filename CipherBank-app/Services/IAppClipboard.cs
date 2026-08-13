// <copyright file="IAppClipboard.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>Platform clipboard adapter for ViewModels (testable; no MAUI statics).</summary>
public interface IAppClipboard
{
    Task SetTextAsync(string text);
}
