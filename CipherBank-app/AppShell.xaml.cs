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
    /// <summary>Minimum time on splash so the pulse mark is visible even when boot is fast.</summary>
    private static readonly TimeSpan MinSplashDuration = TimeSpan.FromMilliseconds(900);

    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.PosLab, typeof(PosLabPage));
        Routing.RegisterRoute(Routes.AddWallet, typeof(AddWalletPage));
        Routing.RegisterRoute(Routes.RestoreBackup, typeof(RestoreBackupPage));
    }

    public AppShell(IAppSession session, ILocalDb db, AppIdleLockService idleLock)
        : this()
    {
        _ = BootstrapAsync(session, db, idleLock);
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
