// <copyright file="LoginViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Constants;
using CipherBank_app.Services;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

/// <summary>
/// ViewModel for the Login page handling user authentication.
/// </summary>
public partial class LoginViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<LoginViewModel> _logger;
    private readonly IProductClient _product;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
#if DEBUG
    private readonly ISettingsService _settings;
#endif
    private CancellationTokenSource? _cts;
    private bool _disposed;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

#if DEBUG
    [ObservableProperty]
    private bool isTestEnvironment;

    [ObservableProperty]
    private string? environmentBadge;

    [ObservableProperty]
    private string? statusMessage;
#endif

#if DEBUG
    public LoginViewModel(
        ILogger<LoginViewModel> logger,
        IProductClient product,
        INavigationService navigation,
        IDialogService dialog,
        ISettingsService settings)
    {
        _logger = logger;
        _product = product;
        _navigation = navigation;
        _dialog = dialog;
        _settings = settings;

        // Check if we're in a test environment
        UpdateEnvironmentIndicator();
    }
#else
    public LoginViewModel(
        ILogger<LoginViewModel> logger,
        IProductClient product,
        INavigationService navigation,
        IDialogService dialog)
    {
        _logger = logger;
        _product = product;
        _navigation = navigation;
        _dialog = dialog;
    }
#endif

    /// <summary>
    /// Cancels the current login operation.
    /// </summary>
    public void CancelLogin()
    {
        _cts?.Cancel();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy)
        {
            return;
        }

        // Cancel any existing operation
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Please enter a username";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter a password";
                return;
            }

            LogAttemptingLogin(_logger, Username);
            await _product.CreateSessionAsync(_cts.Token);
            LogLoginSuccessful(_logger);
            await _navigation.GoToAsync(Routes.Dashboard);
        }
        catch (HttpRequestException ex)
        {
            LogNetworkError(_logger, ex);
            ErrorMessage = "Network error. Please check your connection and try again.";
            await _dialog.ShowAlertAsync("Connection Error", ErrorMessage, "OK");
        }
        catch (OperationCanceledException)
        {
            LogLoginCancelled(_logger);
            ErrorMessage = "Login cancelled";
        }
        catch (InvalidOperationException ex)
        {
            LogInvalidOperation(_logger, ex);
            ErrorMessage = "Invalid credentials or server error";
            await _dialog.ShowAlertAsync("Login Failed", ErrorMessage, "OK");
        }
        catch (Exception ex)
        {
            LogUnexpectedError(_logger, ex);
            ErrorMessage = "An unexpected error occurred. Please try again.";
            await _dialog.ShowAlertAsync("Error", ErrorMessage, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

#if DEBUG
    [RelayCommand]
    private async Task UseTestCredentialsAsync()
    {
        if (_settings.UseMockServices)
        {
            Username = "testuser";
            Password = "password123";
            StatusMessage = "Test credentials filled";
            LogTestCredentialsUsed(_logger);

            // Optionally auto-login
            await SignInAsync();
        }
        else
        {
            await _dialog.ShowAlertAsync(
                "Not Available",
                "Test credentials are only available when using mock services.",
                "OK");
        }
    }

    private void UpdateEnvironmentIndicator()
    {
        IsTestEnvironment = _settings.UseMockServices || _settings.Environment != "Production";

        if (_settings.UseMockServices)
        {
            EnvironmentBadge = "MOCK SERVICES";
        }
        else
        {
            EnvironmentBadge = _settings.Environment.ToUpper(CultureInfo.InvariantCulture) switch
            {
                "SANDBOX" => "SANDBOX",
                "DEVELOPMENT" => "DEV",
                "LOCAL" => "LOCAL",
                _ => null,
            };
        }
    }
#endif

#pragma warning disable SA1204 // Static members should appear before non-static members - LoggerMessage source generators
    [LoggerMessage(Level = LogLevel.Information, Message = "Attempting login for user: {Username}")]
    private static partial void LogAttemptingLogin(ILogger logger, string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Login successful, navigating to main page")]
    private static partial void LogLoginSuccessful(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error during login")]
    private static partial void LogNetworkError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Login operation was cancelled")]
    private static partial void LogLoginCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid operation during login")]
    private static partial void LogInvalidOperation(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error during login")]
    private static partial void LogUnexpectedError(ILogger logger, Exception ex);

#if DEBUG
    [LoggerMessage(Level = LogLevel.Information, Message = "Test credentials used for quick login")]
    private static partial void LogTestCredentialsUsed(ILogger logger);
#endif
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
