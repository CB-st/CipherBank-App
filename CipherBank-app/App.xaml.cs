// <copyright file="App.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.Views;
using Serilog;

namespace CipherBank_app;

/// <summary>
/// The main application class.
/// </summary>
public partial class App
{
    private readonly AppStartupCoordinator _startup;

    public App(AppStartupCoordinator startup)
    {
        InitializeComponent();
        _startup = startup;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        StartupPage page = new();
        Window window = new(page);
        _ = InitializeWindowAsync(window, page);
        return window;
    }

    /// <summary>
    /// Awaits local startup before exposing the product shell.
    /// </summary>
    private async Task InitializeWindowAsync(Window window, StartupPage page)
    {
        try
        {
            await _startup.InitializeAsync(CancellationToken.None);
            window.Page = new AppShell();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Application startup failed");
            page.SetRetry(() => InitializeWindowAsync(window, page));
        }
    }
}
