// <copyright file="MauiProgram.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using CipherBank_app.ChallengePass;
using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.Extensions;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Pos;
using CipherBank_app.V1;
using CipherBank_app.Session;
using CipherBank_app.Wallets;
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
            .RegisterViewModels()
            .RegisterViews()
            .Build();
        // Note: idle lock started from AppShell after services resolve

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

        // Cora port: custody / persist / product API / stream / NFC
        mauiAppBuilder.Services.AddSingleton<ISecureStore, MauiSecureStore>();
        mauiAppBuilder.Services.AddSingleton<IPinService, PinService>();
        mauiAppBuilder.Services.AddSingleton<ICustodyService, CustodyService>();
        mauiAppBuilder.Services.AddSingleton<IBiometricService, BiometricService>();
        mauiAppBuilder.Services.AddSingleton<IStepUpChallenges, MauiStepUpChallenges>();
        mauiAppBuilder.Services.AddSingleton<IStepUpAuth, StepUpAuthService>();
        mauiAppBuilder.Services.AddSingleton<ILocalDb>(sp =>
        {
            string path = Path.Combine(FileSystem.AppDataDirectory, "cipherbank.db");
            return new LocalDb(path);
        });
        mauiAppBuilder.Services.AddSingleton<IWalletRepository, WalletRepository>();
        mauiAppBuilder.Services.AddSingleton<IRecipientRepository, RecipientRepository>();
        mauiAppBuilder.Services.AddSingleton<IPrefsStore, PrefsStore>();
        mauiAppBuilder.Services.AddSingleton<ILocalWalletSeeder, LocalWalletSeeder>();
        mauiAppBuilder.Services.AddSingleton<IProductSessionStore, ProductSessionStorage>();
        mauiAppBuilder.Services.AddSingleton<IAccountKeySource, CustodyAccountKeySource>();
        mauiAppBuilder.Services.AddSingleton<InMemorySessionChallengeClient>();
        mauiAppBuilder.Services.AddSingleton<HttpSessionChallengeClient>();
        mauiAppBuilder.Services.AddSingleton<InMemoryPqKeyShareClient>();
        mauiAppBuilder.Services.AddSingleton<HttpPqKeyShareClient>();
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
                ? new InMemoryPqChannelChallengeSource(
                    sp.GetRequiredService<InMemoryPqKeyShareClient>(),
                    sp.GetRequiredService<ChallengeIdNonceSha256Template>())
                : sp.GetRequiredService<HttpPqChannelChallengeSource>());
#else
        mauiAppBuilder.Services.AddSingleton<ISessionChallengeClient, HttpSessionChallengeClient>();
        mauiAppBuilder.Services.AddSingleton<IPqKeyShareClient, HttpPqKeyShareClient>();
        mauiAppBuilder.Services.AddSingleton<IPqChannelChallengeSource, HttpPqChannelChallengeSource>();
#endif
        mauiAppBuilder.Services.AddChallengePassModule();
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
                    return sp.GetRequiredService<LabSessionProofBuilder>();
            }
        });
        mauiAppBuilder.Services.AddSingleton<MockProductApi>();
        mauiAppBuilder.Services.AddCipherBankHttpClient<HttpProductApi>();
        // Deferred resolve breaks HttpProductApi ↔ challenge/pass client cycle (MS.DI does not auto-wrap Lazy<T>).
        mauiAppBuilder.Services.AddSingleton(sp => new Lazy<IProductApi>(() => sp.GetRequiredService<IProductApi>()));
#if DEBUG
        mauiAppBuilder.Services.AddSingleton<IProductApi>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using MockProductApi (based on settings)");
                return sp.GetRequiredService<MockProductApi>();
            }

            Log.Debug("Using HttpProductApi (live /v1)");
            return sp.GetRequiredService<HttpProductApi>();
        });
        mauiAppBuilder.Services.AddSingleton<IStreamService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using MockStreamService (based on settings)");
                return new MockStreamService();
            }

            Log.Debug("Using ClientWebSocketStreamService");
            return new ClientWebSocketStreamService(settings.StreamEndpoint);
        });
#else
        mauiAppBuilder.Services.AddSingleton<IProductApi>(sp => sp.GetRequiredService<HttpProductApi>());
        mauiAppBuilder.Services.AddSingleton<IStreamService>(sp =>
            new ClientWebSocketStreamService(sp.GetRequiredService<ISettingsService>().StreamEndpoint));
#endif
        mauiAppBuilder.Services.AddSingleton<IStreamHub, StreamHub>();
        mauiAppBuilder.Services.AddSingleton<IPrefsSyncService, PrefsSyncService>();
        mauiAppBuilder.Services.AddSingleton<IAccountBootstrapService, AccountBootstrapService>();
        mauiAppBuilder.Services.AddSingleton<IAppSession, AppSession>();
        mauiAppBuilder.Services.AddSingleton<AppIdleLockService>();
#if ANDROID
        mauiAppBuilder.Services.AddSingleton<INfcPresentmentService, Platforms.Android.Nfc.AndroidNdefPresentmentService>();
#else
        mauiAppBuilder.Services.AddSingleton<INfcPresentmentService, NullNfcPresentmentService>();
#endif


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
        mauiAppBuilder.Services.AddSingleton<MockPublicQuoteService>();

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

        // Public quote surface (/currencies, /quote, /iquote, /test) — separate host from product /v1
        mauiAppBuilder.Services.AddPublicApiHttpClient<PublicApiClient>();

#if DEBUG
        mauiAppBuilder.Services.AddTransient<IPublicQuoteService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMockServices)
            {
                Log.Debug("Using MockPublicQuoteService (based on settings)");
                return sp.GetRequiredService<MockPublicQuoteService>();
            }

            Log.Debug("Using PublicApiClient (real public API)");
            return sp.GetRequiredService<PublicApiClient>();
        });
#else
        mauiAppBuilder.Services.AddTransient<IPublicQuoteService>(sp => sp.GetRequiredService<PublicApiClient>());
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
        mauiAppBuilder.Services.AddTransient<WelcomeViewModel>();
        mauiAppBuilder.Services.AddTransient<KeysViewModel>();
        mauiAppBuilder.Services.AddTransient<BackupQuizViewModel>();
        mauiAppBuilder.Services.AddTransient<SetPinViewModel>();
        mauiAppBuilder.Services.AddTransient<UnlockViewModel>();
        mauiAppBuilder.Services.AddTransient<HomeViewModel>();
        mauiAppBuilder.Services.AddTransient<ConvertViewModel>();
        mauiAppBuilder.Services.AddTransient<SendViewModel>();
        mauiAppBuilder.Services.AddTransient<PayViewModel>();
        mauiAppBuilder.Services.AddTransient<ReceiveViewModel>();
        mauiAppBuilder.Services.AddTransient<ProfileViewModel>();
        mauiAppBuilder.Services.AddTransient<PosLabViewModel>();
        mauiAppBuilder.Services.AddTransient<AddWalletViewModel>();
        // Legacy pages kept registered for optional deep-links
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
        mauiAppBuilder.Services.AddTransient<SplashPage>();
        mauiAppBuilder.Services.AddTransient<WelcomePage>();
        mauiAppBuilder.Services.AddTransient<KeysPage>();
        mauiAppBuilder.Services.AddTransient<BackupQuizPage>();
        mauiAppBuilder.Services.AddTransient<SetPinPage>();
        mauiAppBuilder.Services.AddTransient<UnlockPage>();
        mauiAppBuilder.Services.AddTransient<HomePage>();
        mauiAppBuilder.Services.AddTransient<ConvertPage>();
        mauiAppBuilder.Services.AddTransient<SendPage>();
        mauiAppBuilder.Services.AddTransient<PayPage>();
        mauiAppBuilder.Services.AddTransient<ReceivePage>();
        mauiAppBuilder.Services.AddTransient<ProfilePage>();
        mauiAppBuilder.Services.AddTransient<PosLabPage>();
        mauiAppBuilder.Services.AddTransient<AddWalletPage>();
        mauiAppBuilder.Services.AddTransient<LoginPage>();
        mauiAppBuilder.Services.AddTransient<DashboardPage>();
        mauiAppBuilder.Services.AddTransient<WalletPage>();
        mauiAppBuilder.Services.AddTransient<PurchasePage>();
        mauiAppBuilder.Services.AddTransient<SettingsPage>();

        Log.Information("Views registered successfully");
        return mauiAppBuilder;
    }
}
