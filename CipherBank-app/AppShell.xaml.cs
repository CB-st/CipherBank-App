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
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.PosLab, typeof(PosLabPage));
        Routing.RegisterRoute(Routes.AddWallet, typeof(AddWalletPage));
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
            await db.InitializeAsync();
            await session.BootAsync();
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
}
