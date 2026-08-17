// <copyright file="SettingsService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using CipherBank_app.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Services;

/// <summary>
/// Implementation of application settings using secure preferences storage.
/// </summary>
public sealed partial class SettingsService : ISettingsService
{
    // Preference keys
    private const string IdCipherBankEndpointBase = "cipher_bank_endpoint";
    private const string IdStreamEndpoint = "stream_endpoint";
    private const string IdThemeMode = "theme_mode";
    private const string IdNotificationsEnabled = "notifications_enabled";
    private const string IdBiometricAuthEnabled = "biometric_auth_enabled";
    private const string IdAutoLockTimeout = "auto_lock_timeout";
    private const string IdDefaultCurrency = "default_currency";
    private const string IdSessionProofMode = "session_proof_mode";
    private const string IdEnvironment = "environment";
    private const string IdDeveloperModeEnabled = "developer_mode_enabled";
    private const string IdUseMockServices = "use_mock_services";

    // Default values
    private const string DefaultThemeMode = "System";
    private const bool DefaultNotificationsEnabled = true;
    private const bool DefaultBiometricAuthEnabled = false;
    private const int DefaultAutoLockTimeout = 5;
    private const string DefaultDefaultCurrency = "USD";
    private const SessionProofMode DefaultSessionProofMode = SessionProofMode.Lab;
    private const bool DefaultDeveloperModeEnabled = false;
    private const bool DefaultUseMockServices = true;

    private readonly ILogger<SettingsService>? _logger;
    private readonly NetworkOptions _networkOptions;

    public SettingsService()
        : this(logger: null, NetworkOptions.Default)
    {
    }

    public SettingsService(ILogger<SettingsService> logger)
        : this(logger, NetworkOptions.Default)
    {
    }

    public SettingsService(ILogger<SettingsService> logger, IOptions<NetworkOptions> networkOptions)
        : this(logger, networkOptions.Value)
    {
    }

    private SettingsService(ILogger<SettingsService>? logger, NetworkOptions networkOptions)
    {
        ArgumentNullException.ThrowIfNull(networkOptions);
        _logger = logger;
        _networkOptions = networkOptions;
    }

    public string CipherBankEndpointBase
    {
        get => Preferences.Get(
            IdCipherBankEndpointBase,
            _networkOptions.Resolve(Environment).ApiBase);
        set
        {
            if (_logger != null && _logger.IsEnabled(LogLevel.Information))
            {
                LogSettingChanged(_logger, "CipherBankEndpointBase", value);
            }

            Preferences.Set(IdCipherBankEndpointBase, value);
        }
    }

    public string StreamEndpoint
    {
        get => Preferences.Get(
            IdStreamEndpoint,
            _networkOptions.Resolve(Environment).StreamEndpoint);
        set
        {
            if (_logger != null && _logger.IsEnabled(LogLevel.Information))
            {
                LogSettingChanged(_logger, "StreamEndpoint", value);
            }

            Preferences.Set(IdStreamEndpoint, value);
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

    public SessionProofMode SessionProofMode
    {
        get
        {
            string raw = Preferences.Get(IdSessionProofMode, DefaultSessionProofMode.ToString());
            return Enum.TryParse(raw, ignoreCase: true, out SessionProofMode mode)
                ? mode
                : DefaultSessionProofMode;
        }

        set
        {
            if (_logger != null)
            {
                LogSessionProofModeChanged(_logger, value);
            }

            Preferences.Set(IdSessionProofMode, value.ToString());
        }
    }

    public string Environment
    {
        get => Preferences.Get(IdEnvironment, _networkOptions.DefaultEnvironment);
        set
        {
            if (_logger != null)
            {
                LogSettingChanged(_logger, "Environment", value);
            }

            Preferences.Set(IdEnvironment, value);

            NetworkEnvironmentOptions endpoints = _networkOptions.Resolve(value);
            CipherBankEndpointBase = endpoints.ApiBase;
            StreamEndpoint = endpoints.StreamEndpoint;
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
        NetworkEnvironmentOptions endpoints = _networkOptions.Resolve(_networkOptions.DefaultEnvironment);
        CipherBankEndpointBase = endpoints.ApiBase;
        StreamEndpoint = endpoints.StreamEndpoint;
        ThemeMode = DefaultThemeMode;
        NotificationsEnabled = DefaultNotificationsEnabled;
        BiometricAuthEnabled = DefaultBiometricAuthEnabled;
        AutoLockTimeoutMinutes = DefaultAutoLockTimeout;
        DefaultCurrency = DefaultDefaultCurrency;
        SessionProofMode = DefaultSessionProofMode;
        Environment = _networkOptions.DefaultEnvironment;
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Setting SessionProofMode to {Value}")]
    private static partial void LogSessionProofModeChanged(ILogger logger, SessionProofMode value);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settings reset to defaults")]
    private static partial void LogSettingsResetToDefaults(ILogger logger);
}
