// <copyright file="ShellNavigationService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Shell-based implementation of INavigationService.
/// </summary>
public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) =>
        Shell.Current.GoToAsync(route);

    public Task GoBackAsync() =>
        Shell.Current.GoToAsync("..");
}
