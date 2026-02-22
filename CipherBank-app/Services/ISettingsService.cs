namespace CipherBank_app.Services;

/// <summary>
/// Service for managing application settings and preferences.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets whether mock services should be used instead of real API calls.
    /// </summary>
    bool UseMocks { get; set; }

    /// <summary>
    /// Gets or sets the base URL for the CipherBank API.
    /// </summary>
    string CipherBankEndpointBase { get; set; }

    /// <summary>
    /// Gets or sets the application theme mode (Light, Dark, System).
    /// </summary>
    string ThemeMode { get; set; }

    /// <summary>
    /// Gets or sets whether push notifications are enabled.
    /// </summary>
    bool NotificationsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether biometric authentication is enabled.
    /// </summary>
    bool BiometricAuthEnabled { get; set; }

    /// <summary>
    /// Gets or sets the auto-lock timeout in minutes (0 = disabled).
    /// </summary>
    int AutoLockTimeoutMinutes { get; set; }

    /// <summary>
    /// Gets or sets the default currency for displaying prices.
    /// </summary>
    string DefaultCurrency { get; set; }
}