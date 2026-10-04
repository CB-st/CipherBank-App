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
    private readonly Application _application;

    public MauiAppThemeSetter(IApplication application)
    {
        _application = application as Application
            ?? throw new ArgumentException("The MAUI application must derive from Application.", nameof(application));
    }

    /// <inheritdoc />
    public void SetUserAppTheme(AppTheme theme) => _application.UserAppTheme = theme;
}
