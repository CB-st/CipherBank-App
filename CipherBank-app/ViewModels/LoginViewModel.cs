using System.Net.Http;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

public partial class LoginViewModel(ILogger<LoginViewModel> logger, IAuthService auth) : ObservableObject
{
    [ObservableProperty] string username = string.Empty;
    [ObservableProperty] string password = string.Empty;
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string? errorMessage;

    private CancellationTokenSource? _cts;

    [RelayCommand]
    async Task SignInAsync()
    {
        if (IsBusy) return;

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

            logger.LogInformation("Attempting login for user: {Username}", Username);
            await auth.LoginAsync(Username, Password, _cts.Token);
            logger.LogInformation("Login successful, navigating to main page");
            await Shell.Current.GoToAsync("//DashboardPage");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error during login");
            ErrorMessage = "Network error. Please check your connection and try again.";
            await Shell.Current.DisplayAlertAsync("Connection Error", ErrorMessage, "OK");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Login operation was cancelled");
            ErrorMessage = "Login cancelled";
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Invalid operation during login");
            ErrorMessage = "Invalid credentials or server error";
            await Shell.Current.DisplayAlertAsync("Login Failed", ErrorMessage, "OK");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login");
            ErrorMessage = "An unexpected error occurred. Please try again.";
            await Shell.Current.DisplayAlertAsync("Error", ErrorMessage, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CancelLogin()
    {
        _cts?.Cancel();
    }
}