// <copyright file="MauiAppThemeSetter.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// MAUI adapter that writes <see cref="Application.UserAppTheme"/>.
/// Use: Low (user theme change). Scope: MAUI host.
/// </summary>
public sealed class MauiAppThemeSetter : IAppThemeSetter
{
    /// <inheritdoc />
    public void SetUserAppTheme(AppTheme theme)
    {
        Application? app = Application.Current;
        if (app is not null)
        {
            app.UserAppTheme = theme;
        }
    }
}
