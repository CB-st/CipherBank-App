namespace CipherBank_app;

using System.Net;
using System.Reflection;
using CipherBank_app.Services;
using CipherBank_app.Services.Handlers;
using CipherBank_app.Services.Mocks;
using CipherBank_app.ViewModels;
using CipherBank_app.Views;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Serilog;
using Serilog.Events;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
        => MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
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
        var logPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Logs", "cipherbank-.log");

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

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

        // Register mock services (always available for testing/development)
        mauiAppBuilder.Services.AddSingleton<MockAuthService>();
        mauiAppBuilder.Services.AddSingleton<MockCryptoAPIService>();
        mauiAppBuilder.Services.AddSingleton<MockWalletService>();
        mauiAppBuilder.Services.AddSingleton<MockTransactionService>();

        // Get application version dynamically
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        // Auth Service - Factory pattern for mock/real switching
        mauiAppBuilder.Services.AddHttpClient<AuthService>((serviceProvider, http) =>
        {
            var settings = serviceProvider.GetRequiredService<ISettingsService>();
            http.BaseAddress = new Uri(settings.CipherBankEndpointBase);
            http.Timeout = TimeSpan.FromSeconds(30);

            // Add security headers
            http.DefaultRequestHeaders.Add("Accept", "application/json");
#if DEBUG
            // Only include version/platform headers in debug builds to avoid information disclosure
            http.DefaultRequestHeaders.Add("X-Client-Version", appVersion);
            http.DefaultRequestHeaders.Add("X-Platform", DeviceInfo.Platform.ToString());
#endif
        })
        .ConfigurePrimaryHttpMessageHandler(() => PlatformHttpHandlerFactory.CreateHandler())
        .AddHttpMessageHandler(serviceProvider => new RateLimitingHandler(serviceProvider))
        .AddHttpMessageHandler(serviceProvider => new AuthHeaderHandler(serviceProvider))
        .AddStandardResilienceHandler(ConfigureResilienceOptions);

        mauiAppBuilder.Services.AddTransient<IAuthService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMocks)
            {
                Log.Debug("Using MockAuthService");
                return sp.GetRequiredService<MockAuthService>();
            }
            Log.Debug("Using AuthService");
            return sp.GetRequiredService<AuthService>();
        });

        // Crypto API Service
        mauiAppBuilder.Services.AddHttpClient<CryptoAPIService>((serviceProvider, http) =>
        {
            var settings = serviceProvider.GetRequiredService<ISettingsService>();
            http.BaseAddress = new Uri(settings.CipherBankEndpointBase);
            http.Timeout = TimeSpan.FromSeconds(30);
            http.DefaultRequestHeaders.Add("Accept", "application/json");
#if DEBUG
            http.DefaultRequestHeaders.Add("X-Client-Version", appVersion);
#endif
        })
        .ConfigurePrimaryHttpMessageHandler(() => PlatformHttpHandlerFactory.CreateHandler())
        .AddHttpMessageHandler(serviceProvider => new RateLimitingHandler(serviceProvider))
        .AddHttpMessageHandler(serviceProvider => new AuthHeaderHandler(serviceProvider))
        .AddStandardResilienceHandler(ConfigureResilienceOptions);

        mauiAppBuilder.Services.AddTransient<ICryptoApiService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMocks)
            {
                Log.Debug("Using MockCryptoAPIService");
                return sp.GetRequiredService<MockCryptoAPIService>();
            }
            Log.Debug("Using CryptoAPIService");
            return sp.GetRequiredService<CryptoAPIService>();
        });

        // Wallet Service
        mauiAppBuilder.Services.AddHttpClient<WalletService>((serviceProvider, http) =>
        {
            var settings = serviceProvider.GetRequiredService<ISettingsService>();
            http.BaseAddress = new Uri(settings.CipherBankEndpointBase);
            http.Timeout = TimeSpan.FromSeconds(30);
            http.DefaultRequestHeaders.Add("Accept", "application/json");
#if DEBUG
            http.DefaultRequestHeaders.Add("X-Client-Version", appVersion);
#endif
        })
        .ConfigurePrimaryHttpMessageHandler(() => PlatformHttpHandlerFactory.CreateHandler())
        .AddHttpMessageHandler(serviceProvider => new RateLimitingHandler(serviceProvider))
        .AddHttpMessageHandler(serviceProvider => new AuthHeaderHandler(serviceProvider))
        .AddStandardResilienceHandler(ConfigureResilienceOptions);

        mauiAppBuilder.Services.AddTransient<IWalletService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMocks)
            {
                Log.Debug("Using MockWalletService");
                return sp.GetRequiredService<MockWalletService>();
            }
            Log.Debug("Using WalletService");
            return sp.GetRequiredService<WalletService>();
        });

        // Transaction Service
        mauiAppBuilder.Services.AddHttpClient<TransactionService>((serviceProvider, http) =>
        {
            var settings = serviceProvider.GetRequiredService<ISettingsService>();
            http.BaseAddress = new Uri(settings.CipherBankEndpointBase);
            http.Timeout = TimeSpan.FromSeconds(30);
            http.DefaultRequestHeaders.Add("Accept", "application/json");
#if DEBUG
            http.DefaultRequestHeaders.Add("X-Client-Version", appVersion);
#endif
        })
        .ConfigurePrimaryHttpMessageHandler(() => PlatformHttpHandlerFactory.CreateHandler())
        .AddHttpMessageHandler(serviceProvider => new RateLimitingHandler(serviceProvider))
        .AddHttpMessageHandler(serviceProvider => new AuthHeaderHandler(serviceProvider))
        .AddStandardResilienceHandler(ConfigureResilienceOptions);

        mauiAppBuilder.Services.AddTransient<ITransactionService>(sp =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            if (settings.UseMocks)
            {
                Log.Debug("Using MockTransactionService");
                return sp.GetRequiredService<MockTransactionService>();
            }
            Log.Debug("Using TransactionService");
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
        mauiAppBuilder.Services.AddSingleton<MainPageViewModel>();
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
        mauiAppBuilder.Services.AddSingleton<MainPage>();
        mauiAppBuilder.Services.AddTransient<LoginPage>();
        mauiAppBuilder.Services.AddTransient<DashboardPage>();
        mauiAppBuilder.Services.AddTransient<WalletPage>();
        mauiAppBuilder.Services.AddTransient<PurchasePage>();
        mauiAppBuilder.Services.AddTransient<SettingsPage>();

        Log.Information("Views registered successfully");
        return mauiAppBuilder;
    }

    /// <summary>
    /// Configures resilience options for HTTP clients.
    /// Includes retry with jitter and circuit breaker patterns.
    /// </summary>
    private static void ConfigureResilienceOptions(HttpStandardResilienceOptions options)
    {
        // Retry configuration: 3 attempts with exponential backoff and jitter
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;

        // Only retry on transient errors (network issues, 5xx, 408, 429)
        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Exception is HttpRequestException ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.ServiceUnavailable ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.GatewayTimeout ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.RequestTimeout ||
            args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests ||
            (int?)args.Outcome.Result?.StatusCode >= 500);

        // Circuit breaker: Opens after 50% failure rate, stays open for 30 seconds
        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 10;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

        // Total request timeout
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);

        // Attempt timeout for individual requests
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);

        Log.Debug("Resilience options configured: Retry={MaxRetryAttempts} attempts, CircuitBreaker=50% failure threshold",
            options.Retry.MaxRetryAttempts);
    }
}
