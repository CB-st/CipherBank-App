// <copyright file="SettingsService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services;

/// <summary>
/// Implementation of application settings using secure preferences storage.
/// </summary>
public sealed partial class SettingsService : ISettingsService
{
    // Preference keys
    private const string IdCipherBankEndpointBase = "cipher_bank_endpoint";
    private const string IdThemeMode = "theme_mode";
    private const string IdNotificationsEnabled = "notifications_enabled";
    private const string IdBiometricAuthEnabled = "biometric_auth_enabled";
    private const string IdAutoLockTimeout = "auto_lock_timeout";
    private const string IdDefaultCurrency = "default_currency";
    private const string IdEnvironment = "environment";
    private const string IdDeveloperModeEnabled = "developer_mode_enabled";
    private const string IdUseMockServices = "use_mock_services";

    // Default values
    private const string DefaultCipherBankEndpointBase = "https://api.sandbox.cipherbank.money";
    private const string DefaultThemeMode = "System";
    private const bool DefaultNotificationsEnabled = true;
    private const bool DefaultBiometricAuthEnabled = false;
    private const int DefaultAutoLockTimeout = 5;
    private const string DefaultDefaultCurrency = "USD";
    private const string DefaultEnvironment = "Sandbox";
    private const bool DefaultDeveloperModeEnabled = false;
    private const bool DefaultUseMockServices = true;

    private readonly ILogger<SettingsService>? _logger;

    public SettingsService()
    {
    }

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    public string CipherBankEndpointBase
    {
        get => Preferences.Get(IdCipherBankEndpointBase, DefaultCipherBankEndpointBase);
        set
        {
            if (_logger != null && _logger.IsEnabled(LogLevel.Information))
            {
                LogSettingChanged(_logger, "CipherBankEndpointBase", value);
            }

            Preferences.Set(IdCipherBankEndpointBase, value);
        }
    }

    public string ThemeMode
    {
        get => Preferences.Get(IdThemeMode, DefaultThemeMode);
        set
        {
            if (_logger != null && _logger.IsEnabled(LogLevel.Information))
            {
                LogSettingChanged(_logger, "ThemeMode", value);
            }

            Preferences.Set(IdThemeMode, value);
        }
    }

    public bool NotificationsEnabled
    {
        get => Preferences.Get(IdNotificationsEnabled, DefaultNotificationsEnabled);
        set
        {
            if (_logger != null)
            {
                var valueString = value.ToString();
                LogSettingChanged(_logger, "NotificationsEnabled", valueString);
            }

            Preferences.Set(IdNotificationsEnabled, value);
        }
    }

    public bool BiometricAuthEnabled
    {
        get => Preferences.Get(IdBiometricAuthEnabled, DefaultBiometricAuthEnabled);
        set
        {
            if (_logger != null)
            {
                var valueString = value.ToString();
                LogSettingChanged(_logger, "BiometricAuthEnabled", valueString);
            }

            Preferences.Set(IdBiometricAuthEnabled, value);
        }
    }

    public int AutoLockTimeoutMinutes
    {
        get => Preferences.Get(IdAutoLockTimeout, DefaultAutoLockTimeout);
        set
        {
            if (_logger != null)
            {
                var valueString = value.ToString(CultureInfo.InvariantCulture);
                LogSettingChanged(_logger, "AutoLockTimeoutMinutes", valueString);
            }

            Preferences.Set(IdAutoLockTimeout, value);
        }
    }

    public string DefaultCurrency
    {
        get => Preferences.Get(IdDefaultCurrency, DefaultDefaultCurrency);
        set
        {
            if (_logger != null)
            {
                LogSettingChanged(_logger, "DefaultCurrency", value);
            }

            Preferences.Set(IdDefaultCurrency, value);
        }
    }

    public string Environment
    {
        get => Preferences.Get(IdEnvironment, DefaultEnvironment);
        set
        {
            if (_logger != null)
            {
                LogSettingChanged(_logger, "Environment", value);
            }

            Preferences.Set(IdEnvironment, value);

            // Update endpoint based on environment
            CipherBankEndpointBase = value switch
            {
                "Production" => "https://api.cipherbank.money",
                "Sandbox" => "https://api.sandbox.cipherbank.money",
                "Development" => "https://api.dev.cipherbank.money",
                "Local" => "http://localhost:5000",
                _ => DefaultCipherBankEndpointBase,
            };
        }
    }

#if DEBUG
    public bool DeveloperModeEnabled
    {
        get => Preferences.Get(IdDeveloperModeEnabled, DefaultDeveloperModeEnabled);
        set
        {
            if (_logger != null)
            {
                var valueString = value.ToString();
                LogSettingChanged(_logger, "DeveloperModeEnabled", valueString);
            }

            Preferences.Set(IdDeveloperModeEnabled, value);
        }
    }

    public bool UseMockServices
    {
        get => Preferences.Get(IdUseMockServices, DefaultUseMockServices);
        set
        {
            if (_logger != null)
            {
                var valueString = value.ToString();
                LogSettingChanged(_logger, "UseMockServices", valueString);
            }

            Preferences.Set(IdUseMockServices, value);
        }
    }
#endif

    public void ResetToDefaults()
    {
        CipherBankEndpointBase = DefaultCipherBankEndpointBase;
        ThemeMode = DefaultThemeMode;
        NotificationsEnabled = DefaultNotificationsEnabled;
        BiometricAuthEnabled = DefaultBiometricAuthEnabled;
        AutoLockTimeoutMinutes = DefaultAutoLockTimeout;
        DefaultCurrency = DefaultDefaultCurrency;
        Environment = DefaultEnvironment;
#if DEBUG
        DeveloperModeEnabled = DefaultDeveloperModeEnabled;
        UseMockServices = DefaultUseMockServices;
#endif

        if (_logger != null)
        {
            LogSettingsResetToDefaults(_logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Setting {SettingName} to {Value}")]
    private static partial void LogSettingChanged(ILogger logger, string settingName, string value);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settings reset to defaults")]
    private static partial void LogSettingsResetToDefaults(ILogger logger);
}
