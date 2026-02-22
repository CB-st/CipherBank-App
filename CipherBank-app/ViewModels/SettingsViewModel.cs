using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

/// <summary>
/// ViewModel for the Settings page managing application preferences.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly ISettingsService _settings;
    private readonly IAuthService _authService;
    private CancellationTokenSource? _cts;

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        ISettingsService settings,
        IAuthService authService)
    {
        _logger = logger;
        _settings = settings;
        _authService = authService;

        // Load current settings
        LoadSettings();
    }

    [ObservableProperty]
    private string apiEndpoint = string.Empty;

    [ObservableProperty]
    private bool useMocks;

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

    private void LoadSettings()
    {
        ApiEndpoint = _settings.CipherBankEndpointBase;
        UseMocks = _settings.UseMocks;
        ThemeMode = _settings.ThemeMode;
        NotificationsEnabled = _settings.NotificationsEnabled;
        BiometricEnabled = _settings.BiometricAuthEnabled;
        AutoLockTimeout = _settings.AutoLockTimeoutMinutes;
        DefaultCurrency = _settings.DefaultCurrency;

        _logger.LogInformation("Settings loaded");
    }

    /// <summary>
    /// Saves current settings.
    /// </summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        if (IsSaving) return;

        IsSaving = true;
        StatusMessage = null;

        try
        {
            _logger.LogInformation("Saving settings");

            // Validate API endpoint
            if (!UseMocks && !Uri.TryCreate(ApiEndpoint, UriKind.Absolute, out var uri))
            {
                StatusMessage = "Invalid API endpoint URL";
                IsStatusSuccess = false;
                return;
            }

            _settings.CipherBankEndpointBase = ApiEndpoint;
            _settings.UseMocks = UseMocks;
            _settings.ThemeMode = ThemeMode;
            _settings.NotificationsEnabled = NotificationsEnabled;
            _settings.BiometricAuthEnabled = BiometricEnabled;
            _settings.AutoLockTimeoutMinutes = AutoLockTimeout;
            _settings.DefaultCurrency = DefaultCurrency;

            // Apply theme
            ApplyTheme();

            StatusMessage = "Settings saved successfully";
            IsStatusSuccess = true;

            _logger.LogInformation("Settings saved: UseMocks={UseMocks}, Theme={Theme}",
                UseMocks, ThemeMode);

            await Task.Delay(2000);
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
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
        if (IsTesting) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsTesting = true;
        StatusMessage = "Testing connection...";
        IsStatusSuccess = true;

        try
        {
            _logger.LogInformation("Testing API connection to {Endpoint}", ApiEndpoint);

            using var http = new HttpClient
            {
                BaseAddress = new Uri(ApiEndpoint),
                Timeout = TimeSpan.FromSeconds(10)
            };

            var response = await http.GetAsync("health", _cts.Token);

            if (response.IsSuccessStatusCode)
            {
                StatusMessage = "Connection successful!";
                IsStatusSuccess = true;
                _logger.LogInformation("API connection test successful");
            }
            else
            {
                StatusMessage = $"Connection failed: {response.StatusCode}";
                IsStatusSuccess = false;
                _logger.LogWarning("API connection test failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (TaskCanceledException)
        {
            StatusMessage = "Connection timed out";
            IsStatusSuccess = false;
            _logger.LogWarning("API connection test timed out");
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"Network error: {ex.Message}";
            IsStatusSuccess = false;
            _logger.LogError(ex, "API connection test network error");
        }
        catch (Exception ex)
        {
            StatusMessage = "Connection test failed";
            IsStatusSuccess = false;
            _logger.LogError(ex, "API connection test error");
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
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Reset Settings",
            "Are you sure you want to reset all settings to defaults?",
            "Reset", "Cancel");

        if (!confirm) return;

        _logger.LogInformation("Resetting settings to defaults");

        ApiEndpoint = "https://api.sandbox.cipherbank.money";
        UseMocks = true;
        ThemeMode = "System";
        NotificationsEnabled = true;
        BiometricEnabled = false;
        AutoLockTimeout = 5;
        DefaultCurrency = "USD";

        await SaveSettingsAsync();

        _logger.LogInformation("Settings reset to defaults");
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Log Out",
            "Are you sure you want to log out?",
            "Log Out", "Cancel");

        if (!confirm) return;

        try
        {
            _logger.LogInformation("User logging out");
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//LoginPage");
            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            await Shell.Current.DisplayAlertAsync("Error",
                "Failed to log out. Please try again.", "OK");
        }
    }

    /// <summary>
    /// Opens the about/version info.
    /// </summary>
    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        await Shell.Current.DisplayAlertAsync(
            "CipherBank",
            "Version 1.0.0\n\n" +
            "A secure cryptocurrency wallet and trading application.\n\n" +
            "© 2026 CipherBank. All rights reserved.",
            "OK");
    }

    private void ApplyTheme()
    {
        if (Application.Current == null) return;

        Application.Current.UserAppTheme = ThemeMode switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        _logger.LogDebug("Applied theme: {Theme}", ThemeMode);
    }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        _logger.LogDebug("Settings page disappearing, operations cancelled");
    }
}
