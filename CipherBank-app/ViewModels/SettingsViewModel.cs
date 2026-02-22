// <copyright file="SettingsViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Constants;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

/// <summary>
/// ViewModel for the Settings page managing application preferences.
/// </summary>
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ISettingsService _settings;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
    private readonly IHealthCheckClient _healthCheck;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    [ObservableProperty]
    private string apiEndpoint = string.Empty;

    [ObservableProperty]
    private string themeMode = "System";

    [ObservableProperty]
    private bool notificationsEnabled;

    [ObservableProperty]
    private bool biometricEnabled;

    [ObservableProperty]
    private int autoLockTimeout;

    [ObservableProperty]
    private string defaultCurrency = "USD";

    [ObservableProperty]
    private bool isTesting;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isStatusSuccess;

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ISettingsService settings,
        IAuthService authService,
        INavigationService navigation,
        IDialogService dialog,
        IHealthCheckClient healthCheck)
    {
        _logger = logger;
        _settings = settings;
        _authService = authService;
        _navigation = navigation;
        _dialog = dialog;
        _healthCheck = healthCheck;

        // Load current settings
        LoadSettings();
    }

    /// <summary>
    /// Available theme modes.
    /// </summary>
    public string[] ThemeModes { get; } = ["System", "Light", "Dark"];

    /// <summary>
    /// Available currencies.
    /// </summary>
    public string[] Currencies { get; } = ["USD", "EUR", "GBP", "CAD", "AUD", "JPY"];

    /// <summary>
    /// Available auto-lock timeout options.
    /// </summary>
    public int[] AutoLockOptions { get; } = [0, 1, 5, 15, 30, 60];

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        LogSettingsDisappearing(_logger);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void LoadSettings()
    {
        ApiEndpoint = _settings.CipherBankEndpointBase;
        ThemeMode = _settings.ThemeMode;
        NotificationsEnabled = _settings.NotificationsEnabled;
        BiometricEnabled = _settings.BiometricAuthEnabled;
        AutoLockTimeout = _settings.AutoLockTimeoutMinutes;
        DefaultCurrency = _settings.DefaultCurrency;

        LogSettingsLoaded(_logger);
    }

    /// <summary>
    /// Saves current settings.
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        StatusMessage = null;

        try
        {
            LogSavingSettings(_logger);

            // Validate API endpoint
            if (!Uri.TryCreate(ApiEndpoint, UriKind.Absolute, out _))
            {
                StatusMessage = "Invalid API endpoint URL";
                IsStatusSuccess = false;
                return;
            }

            _settings.CipherBankEndpointBase = ApiEndpoint;
            _settings.ThemeMode = ThemeMode;
            _settings.NotificationsEnabled = NotificationsEnabled;
            _settings.BiometricAuthEnabled = BiometricEnabled;
            _settings.AutoLockTimeoutMinutes = AutoLockTimeout;
            _settings.DefaultCurrency = DefaultCurrency;

            // Apply theme
            ApplyTheme();

            StatusMessage = "Settings saved successfully";
            IsStatusSuccess = true;

            LogSettingsSaved(_logger, ThemeMode);

            await Task.Delay(2000);
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            LogErrorSavingSettings(_logger, ex);
            StatusMessage = "Failed to save settings";
            IsStatusSuccess = false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Tests the API connection.
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsTesting)
        {
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsTesting = true;
        StatusMessage = "Testing connection...";
        IsStatusSuccess = true;

        try
        {
            LogTestingConnection(_logger, ApiEndpoint);

            var isHealthy = await _healthCheck.CheckHealthAsync(ApiEndpoint, _cts.Token);

            if (isHealthy)
            {
                StatusMessage = "Connection successful!";
                IsStatusSuccess = true;
                LogConnectionTestSuccessful(_logger);
            }
            else
            {
                StatusMessage = "Connection failed";
                IsStatusSuccess = false;
                LogConnectionTestFailed(_logger);
            }
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "Connection timed out";
            IsStatusSuccess = false;
            LogConnectionTestTimedOut(_logger);
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Network error: {ex.Message}";
            IsStatusSuccess = false;
            LogConnectionTestNetworkError(_logger, ex);
        }
        catch (Exception ex)
        {
            StatusMessage = "Connection test failed";
            IsStatusSuccess = false;
            LogConnectionTestError(_logger, ex);
        }
        finally
        {
            IsTesting = false;
        }
    }

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var confirm = await _dialog.ShowConfirmAsync(
            "Reset Settings",
            "Are you sure you want to reset all settings to defaults?",
            "Reset",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        LogResettingSettings(_logger);

        _settings.ResetToDefaults();
        LoadSettings();

        await SaveSettingsAsync();

        LogSettingsReset(_logger);
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await _dialog.ShowConfirmAsync(
            "Log Out",
            "Are you sure you want to log out?",
            "Log Out",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        try
        {
            LogUserLoggingOut(_logger);
            await _authService.LogoutAsync();
            await _navigation.GoToAsync(Routes.Login);
            LogUserLoggedOut(_logger);
        }
        catch (Exception ex)
        {
            LogErrorDuringLogout(_logger, ex);
            await _dialog.ShowAlertAsync(
                "Error",
                "Failed to log out. Please try again.",
                "OK");
        }
    }

    /// <summary>
    /// Opens the about/version info.
    /// </summary>
    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        var aboutMessage =
            "Version 1.0.0\n\n" +
            "A secure cryptocurrency wallet and trading application.\n\n" +
            "© 2026 CipherBank. All rights reserved.";
        await _dialog.ShowAlertAsync(
            "CipherBank",
            aboutMessage,
            "OK");
    }

    private void ApplyTheme()
    {
        if (Application.Current == null)
        {
            return;
        }

        Application.Current.UserAppTheme = ThemeMode switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified,
        };

        LogAppliedTheme(_logger, ThemeMode);
    }

#pragma warning disable SA1204 // Static members should appear before non-static members - LoggerMessage source generators
    [LoggerMessage(Level = LogLevel.Information, Message = "Settings loaded")]
    private static partial void LogSettingsLoaded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Saving settings")]
    private static partial void LogSavingSettings(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settings saved: Theme={Theme}")]
    private static partial void LogSettingsSaved(ILogger logger, string theme);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error saving settings")]
    private static partial void LogErrorSavingSettings(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Testing API connection to {Endpoint}")]
    private static partial void LogTestingConnection(ILogger logger, string endpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "API connection test successful")]
    private static partial void LogConnectionTestSuccessful(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API connection test failed")]
    private static partial void LogConnectionTestFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API connection test timed out")]
    private static partial void LogConnectionTestTimedOut(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "API connection test network error")]
    private static partial void LogConnectionTestNetworkError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "API connection test error")]
    private static partial void LogConnectionTestError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resetting settings to defaults")]
    private static partial void LogResettingSettings(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Settings reset to defaults")]
    private static partial void LogSettingsReset(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "User logging out")]
    private static partial void LogUserLoggingOut(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "User logged out successfully")]
    private static partial void LogUserLoggedOut(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during logout")]
    private static partial void LogErrorDuringLogout(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Applied theme: {Theme}")]
    private static partial void LogAppliedTheme(ILogger logger, string theme);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Settings page disappearing, operations cancelled")]
    private static partial void LogSettingsDisappearing(ILogger logger);
#pragma warning restore SA1204 // Static members should appear before non-static members

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cts?.Dispose();
            }

            _disposed = true;
        }
    }
}
