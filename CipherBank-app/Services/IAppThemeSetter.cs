// <copyright file="IAppThemeSetter.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Applies the user-selected application theme so ViewModels do not reach the
/// <c>Application.Current</c> global directly (CB1005).
/// Use: Low (user theme change). Scope: MAUI host.
/// </summary>
public interface IAppThemeSetter
{
    /// <summary>
    /// Applies the requested theme; <see cref="AppTheme.Unspecified"/> follows the system theme.
    /// No-op when the application singleton is not yet available.
    /// </summary>
    void SetUserAppTheme(AppTheme theme);
}
