# MAUI App

The CipherBank-app project is the main .NET MAUI application targeting Android, iOS, Mac Catalyst, and Windows.

## Entry Point

- **App.xaml / App.xaml.cs**: Application class. `CreateWindow` returns a `Window` hosting `AppShell`.
- **MauiProgram.cs**: App builder configuration. Entry point for DI and app setup.

## MauiProgram Configuration

`CreateMauiApp()` chains:

1. **UseMauiApp<App>()** – Sets the application type.
2. **ConfigureFonts()** – OpenSans-Regular, OpenSans-Semibold.
3. **ConfigureLogging()** – Serilog; log level and file sink vary by build (see [config/README.md](../config/README.md)).
4. **RegisterServices()** – All services via `AddCipherBankHttpClient<T>` extension; mock/real by `#if DEBUG`.
5. **RegisterViewModels()** – ViewModels for DI.
6. **RegisterViews()** – Pages for DI.

## Dependency Injection

| Component | Lifetime | Notes |
|-----------|----------|-------|
| ISettingsService | Singleton | Loaded first |
| RateLimiter | Singleton | Shared across HTTP clients |
| INavigationService, IDialogService | Singleton | Shell wrappers |
| IErrorHandler | Singleton | Centralized API error handling |
| IHealthCheckClient | Transient | For Settings Test Connection (cert pinning) |
| Mock services | Singleton | MockAuthService, MockCryptoAPIService, etc. (DEBUG only) |
| IAuthService, ICryptoApiService, etc. | Transient | Resolved by `#if DEBUG` (mocks vs real) |
| AuthService, CryptoAPIService, etc. | Transient | Typed HttpClient via AddCipherBankHttpClient |
| ViewModels | Transient | Per-page |
| Views | Transient | Per-page |

## HTTP Client Resilience

All API clients use `AddStandardResilienceHandler` with:

- **Retry**: 3 attempts, exponential backoff, jitter; retries on transient errors (5xx, 408, 429, HttpRequestException).
- **Circuit breaker**: 50% failure ratio, 10 minimum throughput, 30s sampling, 30s break.
- **Timeouts**: 15s per attempt, 60s total.

## Related Documentation

- [services.md](services.md) – Service implementations
- [viewmodels.md](viewmodels.md) – ViewModels
- [views.md](views.md) – Views/Pages
- [converters.md](converters.md) – Value converters
- [platforms.md](platforms.md) – Platform-specific code
