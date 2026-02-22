// <copyright file="LoginViewModel.cs" company="CipherBank">
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
/// ViewModel for the Login page handling user authentication.
/// </summary>
public partial class LoginViewModel(
    ILogger<LoginViewModel> logger,
    IAuthService auth,
    INavigationService navigation,
    IDialogService dialog) : ObservableObject, IDisposable
{
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

            LogAttemptingLogin(logger, Username);
            await auth.LoginAsync(Username, Password, _cts.Token);
            LogLoginSuccessful(logger);
            await navigation.GoToAsync(Routes.Dashboard);
        }
        catch (HttpRequestException ex)
        {
            LogNetworkError(logger, ex);
            ErrorMessage = "Network error. Please check your connection and try again.";
            await dialog.ShowAlertAsync("Connection Error", ErrorMessage, "OK");
        }
        catch (OperationCanceledException)
        {
            LogLoginCancelled(logger);
            ErrorMessage = "Login cancelled";
        }
        catch (InvalidOperationException ex)
        {
            LogInvalidOperation(logger, ex);
            ErrorMessage = "Invalid credentials or server error";
            await dialog.ShowAlertAsync("Login Failed", ErrorMessage, "OK");
        }
        catch (Exception ex)
        {
            LogUnexpectedError(logger, ex);
            ErrorMessage = "An unexpected error occurred. Please try again.";
            await dialog.ShowAlertAsync("Error", ErrorMessage, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

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
