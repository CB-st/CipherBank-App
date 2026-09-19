// <copyright file="MauiProgram.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using System.Reflection;
using CipherBank_app.Configuration;
using CipherBank_app.Extensions;
using CipherBank_app.Services;
using CipherBank_app.Services.Mocks;
using CipherBank_app.ViewModels;
using CipherBank_app.Views;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CipherBank_app;

/// <summary>
/// The MAUI application program entry point and service registration.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Runtime platform check selects the appsettings.Windows.json overlay; no preprocessor fork.
        string configuration = typeof(MauiProgram).Assembly
            .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration
            ?? "Release";
        bool isDevelopment = string.Equals(configuration, "Debug", StringComparison.OrdinalIgnoreCase);
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.Configuration.AddConfiguration(CipherBankDefaultsConfiguration.BuildForHost(
            isDevelopment,
            OperatingSystem.IsWindows()));

        return builder
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
            .ConfigureMauiHandlers(handlers => handlers.AddPlatformHandlers())
            .ConfigureLogging()
            .RegisterServices()
            .RegisterViewModels()
            .RegisterViews()
            .Build();
    }

    /// <summary>
    /// Configures comprehensive logging with Serilog.
    /// </summary>
    public static MauiAppBuilder ConfigureLogging(this MauiAppBuilder mauiAppBuilder)
    {
        HostBehaviorOptions behavior = mauiAppBuilder.Configuration
            .GetRequiredSection(nameof(HostBehaviorOptions))
            .Get<HostBehaviorOptions>()
            ?? throw new InvalidOperationException("Host behavior configuration is missing.");
        if (!behavior.IsValid()
            || !Enum.TryParse(behavior.MinimumLogLevel, ignoreCase: false, out LogEventLevel minimumLevel))
        {
            throw new InvalidOperationException("Host behavior configuration is invalid.");
        }

        var logPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Logs", "cipherbank-.log");

        LoggerConfiguration config = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext();

        if (behavior.EnableFileLogging)
        {
            config = config.WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
        }

        Logger logger = config.CreateLogger();

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
        mauiAppBuilder.Services.AddPlatformFeatures();
        HostBehaviorOptions behavior = mauiAppBuilder.Configuration
            .GetRequiredSection(nameof(HostBehaviorOptions))
            .Get<HostBehaviorOptions>()
            ?? throw new InvalidOperationException("Host behavior configuration is missing.");
        mauiAppBuilder.Services.AddRequiredOptions(
                mauiAppBuilder.Configuration,
                new HostBehaviorOptions())
            .Validate(static options => options.IsValid(), "Host behavior options are invalid.")
            .ValidateOnStart();
        mauiAppBuilder.Services.AddPersistenceFeature(
            mauiAppBuilder.Configuration,
            new DirectoryInfo(FileSystem.Current.AppDataDirectory));

        // Settings Service (singleton - needed first for other service configuration)
        mauiAppBuilder.Services.AddSingleton<ISettingsService, SettingsService>();

        // Rate Limiter (singleton)
        mauiAppBuilder.Services.AddSingleton<RateLimiter>();

        // Navigation and dialogs
        mauiAppBuilder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        mauiAppBuilder.Services.AddSingleton<IDialogService, ShellDialogService>();

        // Theme port keeps Application.Current out of ViewModels (CB1005).
        mauiAppBuilder.Services.AddSingleton<IAppThemeSetter, MauiAppThemeSetter>();

        // Health check client (for Settings Test Connection)
        mauiAppBuilder.Services.AddHealthCheckClient();

        // Error handler for ViewModel API error consolidation
        mauiAppBuilder.Services.AddSingleton<IErrorHandler, ErrorHandler>();

        // Register mock services (always available for testing/development)
        mauiAppBuilder.Services.AddSingleton<MockAuthService>();
        mauiAppBuilder.Services.AddSingleton<MockCryptoApiService>();
        mauiAppBuilder.Services.AddSingleton<MockWalletService>();
        mauiAppBuilder.Services.AddSingleton<MockTransactionService>();

        // Auth Service - Factory pattern for mock/real switching
        mauiAppBuilder.Services.AddCipherBankHttpClient<AuthService>();

        mauiAppBuilder.Services.AddTransient<IAuthService>(sp =>
        {
            if (behavior.UseMockServices)
            {
                Log.Debug("Using MockAuthService (based on settings)");
                return sp.GetRequiredService<MockAuthService>();
            }

            Log.Debug("Using AuthService (real API)");
            return sp.GetRequiredService<AuthService>();
        });

        // Crypto API Service
        mauiAppBuilder.Services.AddCipherBankHttpClient<CryptoApiService>();

        mauiAppBuilder.Services.AddTransient<ICryptoApiService>(sp =>
        {
            if (behavior.UseMockServices)
            {
                Log.Debug("Using MockCryptoApiService (based on settings)");
                return sp.GetRequiredService<MockCryptoApiService>();
            }

            Log.Debug("Using CryptoApiService (real API)");
            return sp.GetRequiredService<CryptoApiService>();
        });

        // Wallet Service
        mauiAppBuilder.Services.AddCipherBankHttpClient<WalletService>();

        mauiAppBuilder.Services.AddTransient<IWalletService>(sp =>
        {
            if (behavior.UseMockServices)
            {
                Log.Debug("Using MockWalletService (based on settings)");
                return sp.GetRequiredService<MockWalletService>();
            }

            Log.Debug("Using WalletService (real API)");
            return sp.GetRequiredService<WalletService>();
        });

        // Transaction Service
        mauiAppBuilder.Services.AddCipherBankHttpClient<TransactionService>();

        mauiAppBuilder.Services.AddTransient<ITransactionService>(sp =>
        {
            if (behavior.UseMockServices)
            {
                Log.Debug("Using MockTransactionService (based on settings)");
                return sp.GetRequiredService<MockTransactionService>();
            }

            Log.Debug("Using TransactionService (real API)");
            return sp.GetRequiredService<TransactionService>();
        });

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
