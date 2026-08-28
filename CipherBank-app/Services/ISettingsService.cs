// <copyright file="ISettingsService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Service for managing application settings and preferences.
/// </summary>
public interface ISettingsService
{
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

    /// <summary>
    /// Gets or sets the current environment (Production, Sandbox, Development, Local).
    /// </summary>
    string Environment { get; set; }

#if DEBUG
    /// <summary>
    /// Gets or sets whether developer mode is enabled.
    /// </summary>
    bool DeveloperModeEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether to use mock services instead of real API.
    /// </summary>
    bool UseMockServices { get; set; }
#endif

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    void ResetToDefaults();
}
