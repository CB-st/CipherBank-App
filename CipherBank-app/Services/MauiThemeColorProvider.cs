// <copyright file="MauiThemeColorProvider.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
