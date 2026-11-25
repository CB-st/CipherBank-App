// <copyright file="MauiThemeColorProvider.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <inheritdoc />
public sealed class MauiThemeColorProvider : IThemeColorProvider
{
    /// <inheritdoc />
    public Color GetColor(string resourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);
        return Application.Current?.Resources.TryGetValue(resourceKey, out var value) == true
               && value is Color color
            ? color
            : throw new KeyNotFoundException($"Missing semantic color resource '{resourceKey}'.");
    }
}
