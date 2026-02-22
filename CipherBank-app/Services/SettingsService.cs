using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services;

/// <summary>
/// Implementation of application settings using secure preferences storage.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService>? _logger;

    public SettingsService() { }

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    public bool UseMocks
    {
        get => Preferences.Get(IdUseMocks, DefaultUseMocks);
        set
        {
            _logger?.LogInformation("Setting UseMocks to {Value}", value);
            Preferences.Set(IdUseMocks, value);
        }
    }

    public string CipherBankEndpointBase
    {
        get => Preferences.Get(IdCipherBankEndpointBase, DefaultCipherBankEndpointBase);
        set
        {
            _logger?.LogInformation("Setting CipherBankEndpointBase to {Value}", value);
            Preferences.Set(IdCipherBankEndpointBase, value);
        }
    }

    public string ThemeMode
    {
        get => Preferences.Get(IdThemeMode, DefaultThemeMode);
        set
        {
            _logger?.LogInformation("Setting ThemeMode to {Value}", value);
            Preferences.Set(IdThemeMode, value);
        }
    }

    public bool NotificationsEnabled
    {
        get => Preferences.Get(IdNotificationsEnabled, DefaultNotificationsEnabled);
        set
        {
            _logger?.LogInformation("Setting NotificationsEnabled to {Value}", value);
            Preferences.Set(IdNotificationsEnabled, value);
        }
    }

    public bool BiometricAuthEnabled
    {
        get => Preferences.Get(IdBiometricAuthEnabled, DefaultBiometricAuthEnabled);
        set
        {
            _logger?.LogInformation("Setting BiometricAuthEnabled to {Value}", value);
            Preferences.Set(IdBiometricAuthEnabled, value);
        }
    }

    public int AutoLockTimeoutMinutes
    {
        get => Preferences.Get(IdAutoLockTimeout, DefaultAutoLockTimeout);
        set
        {
            _logger?.LogInformation("Setting AutoLockTimeoutMinutes to {Value}", value);
            Preferences.Set(IdAutoLockTimeout, value);
        }
    }

    public string DefaultCurrency
    {
        get => Preferences.Get(IdDefaultCurrency, DefaultDefaultCurrency);
        set
        {
            _logger?.LogInformation("Setting DefaultCurrency to {Value}", value);
            Preferences.Set(IdDefaultCurrency, value);
        }
    }

    // Preference keys
    private const string IdUseMocks = "use_mocks";
    private const string IdCipherBankEndpointBase = "cipher_bank_endpoint";
    private const string IdThemeMode = "theme_mode";
    private const string IdNotificationsEnabled = "notifications_enabled";
    private const string IdBiometricAuthEnabled = "biometric_auth_enabled";
    private const string IdAutoLockTimeout = "auto_lock_timeout";
    private const string IdDefaultCurrency = "default_currency";

    // Default values
#if DEBUG
    private const bool DefaultUseMocks = true; // Default to mocks for development
#else
    private const bool DefaultUseMocks = false; // Production builds use real API
#endif
    private const string DefaultCipherBankEndpointBase = "https://api.sandbox.cipherbank.money";
    private const string DefaultThemeMode = "System";
    private const bool DefaultNotificationsEnabled = true;
    private const bool DefaultBiometricAuthEnabled = false;
    private const int DefaultAutoLockTimeout = 5;
    private const string DefaultDefaultCurrency = "USD";
}