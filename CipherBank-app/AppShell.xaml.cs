// <copyright file="AppShell.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.Views;

namespace CipherBank_app;

/// <summary>Application shell — Cora navigation graph.</summary>
public partial class AppShell : Shell
{
    /// <summary>Minimum time on splash so branding is visible even when boot is fast.</summary>
    private static readonly TimeSpan MinSplashDuration = TimeSpan.FromMilliseconds(900);

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.PosLab, typeof(PosLabPage));
        Routing.RegisterRoute(Routes.AddWallet, typeof(AddWalletPage));
        Routing.RegisterRoute(Routes.RestoreBackup, typeof(RestoreBackupPage));
        Routing.RegisterRoute(Routes.ChangePin, typeof(ChangePinPage));
    }

    public AppShell(IAppSession session, ILocalDb db, AppIdleLockService idleLock)
        : this()
    {
        // Shell.Current is not assigned until this instance is attached as the
        // window's page, so defer navigation-dependent boot work until Loaded
        // fires; otherwise Current.GoToAsync below throws NRE and the app is
        // stuck on whatever ShellContent is declared first in the XAML (Splash).
        // Dispatcher fallback covers rare cases where Loaded is delayed.
        var started = 0;
        void StartBoot()
        {
            if (System.Threading.Interlocked.Exchange(ref started, 1) != 0)
            {
                return;
            }

            _ = BootstrapAsync(session, db, idleLock);
        }

        void OnLoaded(object? sender, EventArgs e)
        {
            Loaded -= OnLoaded;
            StartBoot();
        }

        Loaded += OnLoaded;
        Dispatcher.Dispatch(StartBoot);
    }

    private static async Task BootstrapAsync(IAppSession session, ILocalDb db, AppIdleLockService idleLock)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Current.GoToAsync(Routes.Splash);
            });

            var boot = BootSessionAsync(db, session);
            await Task.WhenAll(boot, Task.Delay(MinSplashDuration)).ConfigureAwait(false);

            idleLock.Start();
            string route = session.HasWallet ? Routes.Unlock : Routes.Welcome;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Current.GoToAsync(route);
            });
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Current.GoToAsync(Routes.Welcome);
            });
        }
    }

    private static async Task BootSessionAsync(ILocalDb db, IAppSession session)
    {
        await db.InitializeAsync().ConfigureAwait(false);
        await session.BootAsync().ConfigureAwait(false);
    }
}
