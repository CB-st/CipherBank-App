// <copyright file="CoraShellFeatureExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ViewModels;
using CipherBank_app.Views;

namespace CipherBank_app.Extensions;

/// <summary>Registers the Cora Shell UI feature as one composition unit.</summary>
public static class CoraShellFeatureExtensions
{
    /// <summary>
    /// Registers Shell pages and their ViewModels.
    /// Use: High (MAUI startup). Scope: application service collection.
    /// </summary>
    public static MauiAppBuilder AddCoraShellFeature(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        AddViewModels(builder.Services);
        AddViews(builder.Services);
        return builder;
    }

    /// <summary>Adds each transient ViewModel owned by the Cora Shell feature.</summary>
    private static void AddViewModels(IServiceCollection services)
    {
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<KeysViewModel>();
        services.AddTransient<BackupQuizViewModel>();
        services.AddTransient<SetPinViewModel>();
        services.AddTransient<UnlockViewModel>();
        services.AddTransient<ChangePinViewModel>();
        services.AddTransient<RestoreBackupViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ConvertViewModel>();
        services.AddTransient<SendViewModel>();
        services.AddTransient<PayViewModel>();
        services.AddTransient<ReceiveViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<PosLabViewModel>();
        services.AddTransient<AddWalletViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<WalletViewModel>();
        services.AddTransient<PurchaseViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    /// <summary>Adds each transient page owned by the Cora Shell feature.</summary>
    private static void AddViews(IServiceCollection services)
    {
        services.AddTransient<SplashPage>();
        services.AddTransient<WelcomePage>();
        services.AddTransient<KeysPage>();
        services.AddTransient<BackupQuizPage>();
        services.AddTransient<SetPinPage>();
        services.AddTransient<UnlockPage>();
        services.AddTransient<ChangePinPage>();
        services.AddTransient<RestoreBackupPage>();
        services.AddTransient<HomePage>();
        services.AddTransient<ConvertPage>();
        services.AddTransient<SendPage>();
        services.AddTransient<PayPage>();
        services.AddTransient<ReceivePage>();
        services.AddTransient<ProfilePage>();
        services.AddTransient<PosLabPage>();
        services.AddTransient<AddWalletPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<DashboardPage>();
        services.AddTransient<WalletPage>();
        services.AddTransient<PurchasePage>();
        services.AddTransient<SettingsPage>();
    }
}
