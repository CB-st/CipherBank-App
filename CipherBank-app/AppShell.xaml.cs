// <copyright file="AppShell.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Constants;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.Views;
using Microsoft.Extensions.DependencyInjection;

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

    public AppShell(
        IServiceProvider services,
        IAppSession session,
        ILocalDb db,
        ICustodyService custody,
        AppIdleLockService idleLock)
        : this()
    {
        ApplyDiPageTemplates(services);

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

            _ = BootstrapAsync(session, db, custody, idleLock);
        }

        void OnLoaded(object? sender, EventArgs e)
        {
            Loaded -= OnLoaded;
            StartBoot();
        }

        Loaded += OnLoaded;
        Dispatcher.Dispatch(StartBoot);
    }

    /// <summary>
    /// Binds ShellContent templates to DI-resolved pages so ctor-injected ViewModels work.
    /// Use: High (once at Shell construction). Scope: hierarchy pages declared in AppShell.xaml.
    /// </summary>
    private void ApplyDiPageTemplates(IServiceProvider services)
    {
        SplashContent.ContentTemplate = DiTemplate<SplashPage>(services);
        WelcomeContent.ContentTemplate = DiTemplate<WelcomePage>(services);
        KeysContent.ContentTemplate = DiTemplate<KeysPage>(services);
        BackupContent.ContentTemplate = DiTemplate<BackupQuizPage>(services);
        SetPinContent.ContentTemplate = DiTemplate<SetPinPage>(services);
        UnlockContent.ContentTemplate = DiTemplate<UnlockPage>(services);
        HomeContent.ContentTemplate = DiTemplate<HomePage>(services);
        ConvertContent.ContentTemplate = DiTemplate<ConvertPage>(services);
        PayContent.ContentTemplate = DiTemplate<PayPage>(services);
        SendContent.ContentTemplate = DiTemplate<SendPage>(services);
        ReceiveContent.ContentTemplate = DiTemplate<ReceivePage>(services);
        ProfileContent.ContentTemplate = DiTemplate<ProfilePage>(services);
    }

    /// <summary>
    /// Builds a DataTemplate that resolves <typeparamref name="TPage"/> from DI.
    /// Use: High (once per ShellContent at construction). Scope: AppShell page factories.
    /// </summary>
    private static DataTemplate DiTemplate<TPage>(IServiceProvider services)
        where TPage : class
        => new(() => services.GetRequiredService<TPage>());

    /// <summary>
    /// Boots local DB / session, then routes to Unlock or Welcome after splash.
    /// Boot failures with an existing seal route to Unlock (never Welcome/create), so a mid-boot
    /// exception cannot open an overwrite path through SetPin.
    /// Use: High (once per cold start). Scope: AppShell bootstrap.
    /// </summary>
    private static async Task BootstrapAsync(
        IAppSession session,
        ILocalDb db,
        ICustodyService custody,
        AppIdleLockService idleLock)
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
            Current.Navigating += (_, _) => idleLock.Touch();
            string route = session.HasWallet ? Routes.Unlock : Routes.Welcome;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Current.GoToAsync(route);
            });
        }
        catch
        {
            string route = await ResolveBootFailureRouteAsync(custody).ConfigureAwait(false);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Current.GoToAsync(route);
            });
        }
    }

    /// <summary>
    /// Prefers Unlock when a custody blob exists; only Welcome when the seal probe confirms none.
    /// If the probe itself fails, Unlock is still preferred over Welcome so create-wallet is not offered.
    /// Use: Low (bootstrap catch). Scope: AppShell bootstrap.
    /// </summary>
    private static async Task<string> ResolveBootFailureRouteAsync(ICustodyService custody)
    {
        try
        {
            return await custody.HasSealedWalletAsync().ConfigureAwait(false)
                ? Routes.Unlock
                : Routes.Welcome;
        }
        catch
        {
            return Routes.Unlock;
        }
    }

    /// <summary>
    /// Initializes the local DB then boots the custody session.
    /// Use: High (once per cold start). Scope: AppShell bootstrap.
    /// </summary>
    private static async Task BootSessionAsync(ILocalDb db, IAppSession session)
    {
        await db.InitializeAsync().ConfigureAwait(false);
        await session.BootAsync().ConfigureAwait(false);
    }
}
