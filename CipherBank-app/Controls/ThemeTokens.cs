// <copyright file="ThemeTokens.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Controls;

/// <summary>
/// Resolves design-token colors from the merged application resources so controls
/// never duplicate the hex values defined in Colors.xaml.
/// </summary>
internal static class ThemeTokens
{
    public static Color Get(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Colors.Transparent;
}
