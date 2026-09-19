// <copyright file="MauiProgram.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Configuration;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using CipherBank_app.Extensions;
using CipherBank_app.Persist;
using CipherBank_app.Pos;
using CipherBank_app.Session;
using CipherBank_app.Services;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using Microsoft.Extensions.Configuration;
using Plugin.Maui.Biometric;
using Serilog;
using Serilog.Events;

namespace CipherBank_app;

/// <summary>
/// The MAUI application program entry point and service registration.
/// </summary>
public static class MauiProgram
{
    // Rebase note: preserve M2's per-TFM platform DI registration, shared
    // CertificatePinPolicy, fail-closed transport, injected motion preference,
    // and native/simulated glass handlers; keep shared host code preprocessor-free.
    public static MauiApp CreateMauiApp()
    {
#if DEBUG
        const bool IsDevelopment = true;
#else
        const bool IsDevelopment = false;
#endif

        // Runtime platform check selects the appsettings.Windows.json overlay; no preprocessor fork.
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.Configuration.AddConfiguration(CipherBankDefaultsConfiguration.BuildForHost(
            IsDevelopment,
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
                fonts.AddFont("Manrope-Regular.ttf", "ManropeRegular");
                fonts.AddFont("Manrope-SemiBold.ttf", "ManropeSemiBold");
                fonts.AddFont("Manrope-Bold.ttf", "ManropeBold");
                fonts.AddFont("Manrope-ExtraBold.ttf", "ManropeExtraBold");
                fonts.AddFont("SpaceMono-Regular.ttf", "SpaceMonoRegular");
                fonts.AddFont("SpaceMono-Bold.ttf", "SpaceMonoBold");
                // Legacy aliases (Inter → Manrope) so older XAML keeps resolving
                fonts.AddFont("Manrope-Regular.ttf", "InterRegular");
                fonts.AddFont("Manrope-SemiBold.ttf", "InterMedium");
                fonts.AddFont("Manrope-SemiBold.ttf", "InterSemiBold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if IOS || MACCATALYST
                handlers.AddHandler<Controls.BlurBackdropView, Handlers.BlurBackdropViewHandler>();
#endif
            })
            .ConfigureLogging()
            .RegisterServices()
            .AddCoraShellFeature()
            .Build();
    }

    // The idle-lock service starts after AppShell resolves the application graph.

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

        LoggerConfiguration config = new LoggerConfiguration()
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
        mauiAppBuilder.Configuration.AddConfiguration(ChallengePassDefaultsConfiguration.Build());
        mauiAppBuilder.Services.AddCipherBankCore(
            mauiAppBuilder.Configuration,
            FileSystem.Current.AppDataDirectory);

        // Settings Service (singleton - needed first for other service configuration)
        mauiAppBuilder.Services.AddSingleton<ISettingsService, SettingsService>();
        mauiAppBuilder.Services.AddSingleton<IThemeColorProvider, MauiThemeColorProvider>();
        mauiAppBuilder.Services.AddSingleton<IUiDispatcher, MauiUiDispatcher>();
        mauiAppBuilder.Services.AddSingleton<IPosCardSelectionStore, MauiPosCardSelectionStore>();

        // Theme port keeps Application.Current out of ViewModels (CB1005).
        mauiAppBuilder.Services.AddSingleton<IAppThemeSetter, MauiAppThemeSetter>();
        mauiAppBuilder.Services.AddSingleton<TaskScheduler>(TaskScheduler.Default);

        // MAUI adapters and Core coordinators.
        mauiAppBuilder.Services.AddSingleton<ISecureStore, MauiSecureStore>();
        mauiAppBuilder.Services.AddSingleton<IPinService, PinService>();
        mauiAppBuilder.Services.AddSingleton<ICustodyService, CustodyService>();
        mauiAppBuilder.Services.AddSingleton<PinChangeCoordinator>();
        mauiAppBuilder.Services.AddSingleton(_ => BiometricAuthenticationService.Default);
        mauiAppBuilder.Services.AddSingleton<IBiometricService, BiometricService>();
        mauiAppBuilder.Services.AddSingleton<IStepUpChallenges, MauiStepUpChallenges>();
        mauiAppBuilder.Services.AddSingleton<IStepUpAuth, StepUpAuthService>();
        mauiAppBuilder.Services.AddSingleton<IMnemonicBackupService, MnemonicBackupService>();
        mauiAppBuilder.Services.AddSingleton<IBackupFileService, BackupFileService>();
        mauiAppBuilder.Services.AddSingleton<ILocalWalletSeeder, LocalWalletSeeder>();
        mauiAppBuilder.Services.AddSingleton<IProductSessionStore, ProductSessionStorage>();
        mauiAppBuilder.Services.AddSingleton<IAccountKeySource, CustodyAccountKeySource>();

        // Challenge/pass dependencies are selected at the composition root.
        mauiAppBuilder.Services.AddSingleton<InMemorySessionChallengeClient>();
        mauiAppBuilder.Services.AddSingleton<HttpSessionChallengeClient>();
        mauiAppBuilder.Services.AddSingleton<InMemoryPqKeyShareClient>();
        mauiAppBuilder.Services.AddSingleton<HttpPqKeyShareClient>();
        mauiAppBuilder.Services.AddSingleton<InMemoryPqChannelChallengeSource>();
        mauiAppBuilder.Services.AddSingleton<HttpPqChannelChallengeSource>();
#if DEBUG
        mauiAppBuilder.Services.AddSingleton<ISessionChallengeClient>(sp =>
            sp.GetRequiredService<ISettingsService>().UseMockServices
                ? sp.GetRequiredService<InMemorySessionChallengeClient>()
                : sp.GetRequiredService<HttpSessionChallengeClient>());
        mauiAppBuilder.Services.AddSingleton<IPqKeyShareClient>(sp =>
            sp.GetRequiredService<ISettingsService>().UseMockServices
                ? sp.GetRequiredService<InMemoryPqKeyShareClient>()
                : sp.GetRequiredService<HttpPqKeyShareClient>());
        mauiAppBuilder.Services.AddSingleton<IPqChannelChallengeSource>(sp =>
            sp.GetRequiredService<ISettingsService>().UseMockServices
                ? sp.GetRequiredService<InMemoryPqChannelChallengeSource>()
                : sp.GetRequiredService<HttpPqChannelChallengeSource>());
#else
        mauiAppBuilder.Services.AddSingleton<ISessionChallengeClient, HttpSessionChallengeClient>();
        mauiAppBuilder.Services.AddSingleton<IPqKeyShareClient, HttpPqKeyShareClient>();
        mauiAppBuilder.Services.AddSingleton<IPqChannelChallengeSource, HttpPqChannelChallengeSource>();
#endif
        mauiAppBuilder.Services.AddChallengePassModule(mauiAppBuilder.Configuration);
        mauiAppBuilder.Services.AddSingleton<ISessionProofBuilder>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            var catalog = sp.GetRequiredService<IChallengePassCatalog>();
            switch (settings.SessionProofMode)
            {
                case SessionProofMode.ChallengePassA2:
                    catalog.SetActive(ChallengePassServiceCollectionExtensions.SuiteA2Id);
                    return sp.GetRequiredService<ChallengePassSessionProofBuilder>();
                case SessionProofMode.ChallengePassA1:
                    catalog.SetActive(ChallengePassServiceCollectionExtensions.SuiteA1Id);
                    return sp.GetRequiredService<ChallengePassSessionProofBuilder>();
                default:
                    throw new InvalidOperationException(
                        "MAUI host requires SessionProofMode ChallengePass A1 or A2. Lab proofs stay on Core test DI; they are not a shipping default.");
            }
        });
        mauiAppBuilder.Services.AddSingleton<InMemoryProductClient>();
        mauiAppBuilder.Services.AddCipherBankHttpClient<HttpProductClient>();
        // Deferred resolve breaks HttpProductClient ↔ challenge/pass client cycle (MS.DI does not auto-wrap Lazy<T>).
        mauiAppBuilder.Services.AddSingleton(sp => new Lazy<IProductClient>(() => sp.GetRequiredService<IProductClient>()));
#if DEBUG
        mauiAppBuilder.Services.AddSingleton<IProductClient>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using InMemoryProductClient (based on settings)");
                return sp.GetRequiredService<InMemoryProductClient>();
            }

            Log.Debug("Using HttpProductClient (live /v1)");
            return sp.GetRequiredService<HttpProductClient>();
        });
        mauiAppBuilder.Services.AddSingleton<InMemoryStreamService>();
        mauiAppBuilder.Services.AddSingleton<IStreamService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using InMemoryStreamService (based on settings)");
                return sp.GetRequiredService<InMemoryStreamService>();
            }

            Log.Debug("Using ClientWebSocketStreamService");
            return new ClientWebSocketStreamService(settings.StreamEndpoint);
        });
#else
        mauiAppBuilder.Services.AddSingleton<IProductClient>(sp => sp.GetRequiredService<HttpProductClient>());
        mauiAppBuilder.Services.AddSingleton<IStreamService>(sp =>
            new ClientWebSocketStreamService(sp.GetRequiredService<ISettingsService>().StreamEndpoint));
#endif
        mauiAppBuilder.Services.AddSingleton<IStreamHub, StreamHub>();
        mauiAppBuilder.Services.AddSingleton<IPrefsSyncService, PrefsSyncService>();
        mauiAppBuilder.Services.AddSingleton<IAccountBootstrapService, AccountBootstrapService>();
        mauiAppBuilder.Services.AddSingleton<IProductSessionCoordinator, ProductSessionCoordinator>();
        mauiAppBuilder.Services.AddSingleton<IAppSession, AppSession>();
        mauiAppBuilder.Services.AddSingleton<AppIdleLockService>();
#if ANDROID
        mauiAppBuilder.Services.AddSingleton<INfcPresentmentService, Platforms.Android.Nfc.AndroidNdefPresentmentService>();
#else
        mauiAppBuilder.Services.AddSingleton<INfcPresentmentService, NullNfcPresentmentService>();
#endif

        // Navigation and dialogs
        mauiAppBuilder.Services.AddSingleton<OnboardingMnemonicHold>();
        mauiAppBuilder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        mauiAppBuilder.Services.AddSingleton<IDialogService, ShellDialogService>();
        mauiAppBuilder.Services.AddSingleton<IAppClipboard, MauiAppClipboard>();

        // Health check client (for Settings Test Connection)
        mauiAppBuilder.Services.AddHealthCheckClient();

        // Error handler for ViewModel API error consolidation
        mauiAppBuilder.Services.AddSingleton<IErrorHandler, ErrorHandler>();

        mauiAppBuilder.Services.AddSingleton<InMemoryPublicQuoteService>();

        // Public quote surface (/currencies, /quote, /iquote, /test) — separate host from product /v1
        mauiAppBuilder.Services.AddPublicApiHttpClient<PublicApiClient>();

#if DEBUG
        mauiAppBuilder.Services.AddTransient<IPublicQuoteService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using InMemoryPublicQuoteService (based on settings)");
                return sp.GetRequiredService<InMemoryPublicQuoteService>();
            }

            Log.Debug("Using PublicApiClient (real public API)");
            return sp.GetRequiredService<PublicApiClient>();
        });
#else
        mauiAppBuilder.Services.AddTransient<IPublicQuoteService>(sp => sp.GetRequiredService<PublicApiClient>());
#endif
        mauiAppBuilder.Services.AddSingleton<MarketRateHydrator>();

        Log.Information("Services registered successfully");
        return mauiAppBuilder;
    }
}
