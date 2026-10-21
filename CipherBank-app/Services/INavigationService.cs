// <copyright file="INavigationService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Abstraction for application navigation, enabling testability without Shell dependency.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the current route, or null when navigation has not started.
    /// Used to avoid sending the user to Login when they are already there.
    /// </summary>
    string? CurrentRoute { get; }

    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    Task GoToAsync(string route);

    /// <summary>
    /// Navigates back in the navigation stack.
    /// </summary>
    Task GoBackAsync();
}
