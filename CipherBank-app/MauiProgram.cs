// <copyright file="MauiProgram.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using CipherBank_app.Extensions;
using CipherBank_app.Services;
using CipherBank_app.Services.Mocks;
using CipherBank_app.ViewModels;
using CipherBank_app.Views;
using Serilog;
using Serilog.Events;

namespace CipherBank_app;

/// <summary>
/// The MAUI application program entry point and service registration.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
        => MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SpaceGrotesk-Medium.ttf", "SpaceGroteskMedium");
                fonts.AddFont("SpaceGrotesk-SemiBold.ttf", "SpaceGroteskSemiBold");
                fonts.AddFont("SpaceGrotesk-Bold.ttf", "SpaceGroteskBold");
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
            })
            .ConfigureLogging()
            .RegisterServices()
            .RegisterViewModels()
            .RegisterViews()
            .Build();

    /// <summary>
    /// Configures comprehensive logging with Serilog.
    /// </summary>
    public static MauiAppBuilder ConfigureLogging(this MauiAppBuilder mauiAppBuilder)
    {
#if DEBUG
        var minimumLevel = LogEventLevel.Debug;
        var logPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Logs", "cipherbank-.log");
#else
        var minimumLevel = LogEventLevel.Information;
        var logPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Logs", "cipherbank-.log");
#endif

        var config = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext();

#if DEBUG
        config = config.WriteTo.File(
            logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            formatProvider: CultureInfo.InvariantCulture,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
#else
        // In Release: file sink enabled for diagnostics; consider disabling for privacy
        config = config.WriteTo.File(
            logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 3,
            restrictedToMinimumLevel: LogEventLevel.Warning,
            formatProvider: CultureInfo.InvariantCulture,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
#endif

        var logger = config.CreateLogger();

        mauiAppBuilder.Services.AddSerilog(logger);

        Log.Logger = logger;
        Log.Information("CipherBank application starting");

        return mauiAppBuilder;
    }

    /// <summary>
    /// Registers all application services with dependency injection.
    /// Supports dynamic switching between mock and real implementations based on settings.
    /// </summary>
    public static MauiAppBuilder RegisterServices(this MauiAppBuilder mauiAppBuilder)
    {
        // Settings Service (singleton - needed first for other service configuration)
        mauiAppBuilder.Services.AddSingleton<ISettingsService, SettingsService>();

        // Rate Limiter (singleton)
        mauiAppBuilder.Services.AddSingleton<RateLimiter>();

        // Navigation and dialogs
        mauiAppBuilder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        mauiAppBuilder.Services.AddSingleton<IDialogService, ShellDialogService>();

        // Health check client (for Settings Test Connection)
        mauiAppBuilder.Services.AddHealthCheckClient();

        // Error handler for ViewModel API error consolidation
        mauiAppBuilder.Services.AddSingleton<IErrorHandler, ErrorHandler>();

        // Register mock services (always available for testing/development)
        mauiAppBuilder.Services.AddSingleton<MockAuthService>();
        mauiAppBuilder.Services.AddSingleton<MockCryptoAPIService>();
        mauiAppBuilder.Services.AddSingleton<MockWalletService>();
        mauiAppBuilder.Services.AddSingleton<MockTransactionService>();

        // Auth Service - Factory pattern for mock/real switching
        mauiAppBuilder.Services.AddCipherBankHttpClient<AuthService>();

#if DEBUG
        mauiAppBuilder.Services.AddTransient<IAuthService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using MockAuthService (based on settings)");
                return sp.GetRequiredService<MockAuthService>();
            }
            else
            {
                Log.Debug("Using AuthService (real API)");
                return sp.GetRequiredService<AuthService>();
            }
        });
#else
        mauiAppBuilder.Services.AddTransient<IAuthService>(sp => sp.GetRequiredService<AuthService>());
#endif

        // Crypto API Service
        mauiAppBuilder.Services.AddCipherBankHttpClient<CryptoAPIService>();

#if DEBUG
        mauiAppBuilder.Services.AddTransient<ICryptoApiService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using MockCryptoAPIService (based on settings)");
                return sp.GetRequiredService<MockCryptoAPIService>();
            }
            else
            {
                Log.Debug("Using CryptoAPIService (real API)");
                return sp.GetRequiredService<CryptoAPIService>();
            }
        });
#else
        mauiAppBuilder.Services.AddTransient<ICryptoApiService>(sp => sp.GetRequiredService<CryptoAPIService>());
#endif

        // Wallet Service
        mauiAppBuilder.Services.AddCipherBankHttpClient<WalletService>();

#if DEBUG
        mauiAppBuilder.Services.AddTransient<IWalletService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using MockWalletService (based on settings)");
                return sp.GetRequiredService<MockWalletService>();
            }
            else
            {
                Log.Debug("Using WalletService (real API)");
                return sp.GetRequiredService<WalletService>();
            }
        });
#else
        mauiAppBuilder.Services.AddTransient<IWalletService>(sp => sp.GetRequiredService<WalletService>());
#endif

        // Transaction Service
        mauiAppBuilder.Services.AddCipherBankHttpClient<TransactionService>();

#if DEBUG
        mauiAppBuilder.Services.AddTransient<ITransactionService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using MockTransactionService (based on settings)");
                return sp.GetRequiredService<MockTransactionService>();
            }
            else
            {
                Log.Debug("Using TransactionService (real API)");
                return sp.GetRequiredService<TransactionService>();
            }
        });
#else
        mauiAppBuilder.Services.AddTransient<ITransactionService>(sp => sp.GetRequiredService<TransactionService>());
#endif

        Log.Information("Services registered successfully");
        return mauiAppBuilder;
    }

    /// <summary>
    /// Registers all ViewModels with dependency injection.
    /// </summary>
    public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddTransient<LoginViewModel>();
        mauiAppBuilder.Services.AddTransient<DashboardViewModel>();
        mauiAppBuilder.Services.AddTransient<WalletViewModel>();
        mauiAppBuilder.Services.AddTransient<PurchaseViewModel>();
        mauiAppBuilder.Services.AddTransient<SettingsViewModel>();

        Log.Information("ViewModels registered successfully");
        return mauiAppBuilder;
    }

    /// <summary>
    /// Registers all Views/Pages with dependency injection.
    /// </summary>
    public static MauiAppBuilder RegisterViews(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.AddTransient<LoginPage>();
        mauiAppBuilder.Services.AddTransient<DashboardPage>();
        mauiAppBuilder.Services.AddTransient<WalletPage>();
        mauiAppBuilder.Services.AddTransient<PurchasePage>();
        mauiAppBuilder.Services.AddTransient<SettingsPage>();

        Log.Information("Views registered successfully");
        return mauiAppBuilder;
    }
}
