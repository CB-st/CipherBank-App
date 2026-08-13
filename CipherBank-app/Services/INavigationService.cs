// <copyright file="INavigationService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Abstraction for application navigation, enabling testability without Shell dependency.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    Task GoToAsync(string route);

    /// <summary>
    /// Navigates back in the navigation stack.
    /// </summary>
    Task GoBackAsync();
}
