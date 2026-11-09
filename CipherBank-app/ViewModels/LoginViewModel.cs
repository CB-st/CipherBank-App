// <copyright file="LoginViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

#if DEBUG
using System.Globalization;
using CipherBank_app.Configuration;
#endif
using CipherBank_app.Constants;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
#if DEBUG
using Microsoft.Extensions.Options;
#endif

namespace CipherBank_app.ViewModels;

/// <summary>
/// ViewModel for the Login page handling user authentication.
/// </summary>
public partial class LoginViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<LoginViewModel> _logger;
    private readonly IAppSession _session;
    private readonly ProductSessionAuthority? _authority;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
#if DEBUG
    private readonly ISettingsService _settings;
    private readonly bool _useMockServices;
#endif
    private readonly object _loginGate = new();
    private CancellationTokenSource? _cts;
    private bool _disposed;

#if DEBUG
    public LoginViewModel(
        ILogger<LoginViewModel> logger,
        IAppSession session,
        INavigationService navigation,
        IDialogService dialog,
        ISettingsService settings,
        IOptions<HostBehaviorOptions> hostBehavior,
        ProductSessionAuthority? authority = null)
    {
        _logger = logger;
        _session = session;
        _authority = authority;
        _navigation = navigation;
        _dialog = dialog;
        _settings = settings;
        _useMockServices = hostBehavior.Value.UseMockServices;

        // Check if we're in a test environment
        UpdateEnvironmentIndicator();
    }
#else
    public LoginViewModel(
        ILogger<LoginViewModel> logger,
        IAppSession session,
        INavigationService navigation,
        IDialogService dialog,
        ProductSessionAuthority? authority = null)
    {
        _logger = logger;
        _session = session;
        _authority = authority;
        _navigation = navigation;
        _dialog = dialog;
    }
#endif

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

#if DEBUG
    [ObservableProperty]
    public partial bool IsTestEnvironment { get; set; }

    [ObservableProperty]
    public partial string? EnvironmentBadge { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }
#endif

    /// <summary>
    /// Cancels the current login operation.
    /// </summary>
    public void CancelLogin()
    {
        lock (_loginGate)
        {
            _cts?.Cancel();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void RollbackLogin(ProductSessionOperation? identity)
    {
        if (identity is not null)
        {
            _authority!.Invalidate(identity, _session.Lock);
        }
        else
        {
            _session.Lock();
        }
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        CancellationTokenSource operation;
        lock (_loginGate)
        {
            if (_disposed || _cts is not null)
            {
                return;
            }

            operation = new CancellationTokenSource();
            _cts = operation;
        }

        IsBusy = true;
        ErrorMessage = null;
        ProductSessionOperation? identity = null;

        try
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter your custody PIN";
                return;
            }

            LogAttemptingLogin(_logger);

            // IAppSession has no cancellation overload: observe completion, then roll back
            // cancelled unlocks before navigation. Never abandon a live startup task.
            bool unlocked;
            if (_session is AppSession app && _authority is not null)
            {
                identity = _authority.Begin();
                unlocked = await app.UnlockAsync(Password, identity);
            }
            else
            {
                unlocked = await _session.UnlockAsync(Password);
            }

            operation.Token.ThrowIfCancellationRequested();

            if (!unlocked)
            {
                ErrorMessage = "Unable to unlock. Check your custody PIN and session configuration.";
                return;
            }

            // Navigation is the commit point; page disappearance during this transition
            // must not undo the completed session.
            LogLoginSuccessful(_logger);
            if (identity is not null)
            {
                Task transition = Task.CompletedTask;
                _authority!.Commit(identity, () =>
                {
                    operation.Token.ThrowIfCancellationRequested();
                    transition = _navigation.GoToAsync(Routes.Dashboard);
                });
                await transition;
            }
            else
            {
                await _navigation.GoToAsync(Routes.Dashboard);
            }
        }
        catch (HttpRequestException ex)
        {
            RollbackLogin(identity);
            LogNetworkError(_logger, ex);
            ErrorMessage = "Network error. Please check your connection and try again.";
            await _dialog.ShowAlertAsync("Connection Error", ErrorMessage, "OK");
        }
        catch (OperationCanceledException)
        {
            RollbackLogin(identity);
            LogLoginCancelled(_logger);
            ErrorMessage = "Login cancelled";
        }
        catch (InvalidOperationException ex)
        {
            RollbackLogin(identity);
            LogInvalidOperation(_logger, ex);
            ErrorMessage = "Invalid credentials or server error";
            await _dialog.ShowAlertAsync("Login Failed", ErrorMessage, "OK");
        }
        catch (Exception ex)
        {
            RollbackLogin(identity);
            LogUnexpectedError(_logger, ex);
            ErrorMessage = "An unexpected error occurred. Please try again.";
            await _dialog.ShowAlertAsync("Error", ErrorMessage, "OK");
        }
        finally
        {
            Password = string.Empty;

            // Serialize cancellation with disposal; the awaited operation retains ownership.
            lock (_loginGate)
            {
                operation.Dispose();
                _cts = null;
            }

            IsBusy = false;
        }
    }

#if DEBUG
    [RelayCommand]
    private async Task UseTestCredentialsAsync()
    {
        if (_useMockServices)
        {
            Username = "testuser";
            Password = string.Empty;
            LogTestCredentialsUsed(_logger);

            // Custody PINs are device-specific, including in development.
            StatusMessage = "Enter the custody PIN for this device";
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
        IsTestEnvironment = _useMockServices || _settings.Environment != "Production";

        if (_useMockServices)
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
    [LoggerMessage(Level = LogLevel.Information, Message = "Attempting custody unlock")]
    private static partial void LogAttemptingLogin(ILogger logger);

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
        lock (_loginGate)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    // SignInAsync owns disposal after the awaited lifecycle finishes.
                    _cts?.Cancel();
                }
            }
        }
    }
}
