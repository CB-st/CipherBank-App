// <copyright file="App.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.Views;
using Serilog;

namespace CipherBank_app;

/// <summary>
/// The main application class.
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly IAppSession _session;
    private readonly AppStartupCoordinator _startup;
    private readonly ICustodyService _custody;
    private readonly AppIdleLockService _idleLock;

    public App(
        IServiceProvider services,
        IAppSession session,
        AppStartupCoordinator startup,
        ICustodyService custody,
        AppIdleLockService idleLock)
    {
        InitializeComponent();
        _services = services;
        _session = session;
        _startup = startup;
        _custody = custody;
        _idleLock = idleLock;
        UserAppTheme = AppTheme.Dark;
    }

    /// <summary>
    /// Keeps required database initialization and seeding ahead of the Cora shell.
    /// Use: Low (window creation). Scope: application startup.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        StartupPage page = new();
        Window window = new(page);
        _ = InitializeWindowAsync(window, page);
        return window;
    }

    /// <summary>
    /// Retries local startup before exposing custody/session routing in the shell.
    /// Use: Low (cold start or user retry). Scope: this window.
    /// </summary>
    private async Task InitializeWindowAsync(Window window, StartupPage page)
    {
        try
        {
            await _startup.InitializeAsync(CancellationToken.None);
            window.Page = new AppShell(_services, _session, _startup, _custody, _idleLock);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Application startup failed");
            page.SetRetry(() => InitializeWindowAsync(window, page));
        }
    }
}
