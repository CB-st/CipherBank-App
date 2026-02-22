using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page displaying cryptocurrency prices and market data.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly ICryptoApiService _cryptoService;
    private CancellationTokenSource? _cts;

    public DashboardViewModel(ILogger<DashboardViewModel> logger, ICryptoApiService cryptoService)
    {
        _logger = logger;
        _cryptoService = cryptoService;
    }

    [ObservableProperty]
    private ObservableCollection<CryptoCurrency> cryptocurrencies = [];

    [ObservableProperty]
    private CryptoCurrency? selectedCrypto;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private decimal totalPortfolioValue;

    /// <summary>
    /// Loads cryptocurrency prices from the API.
    /// </summary>
    [RelayCommand]
    private async Task LoadPricesAsync()
    {
        if (IsLoading) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _logger.LogInformation("Loading cryptocurrency prices");
            var cryptos = await _cryptoService.GetCryptoPricesAsync(_cts.Token);

            Cryptocurrencies.Clear();
            foreach (var crypto in cryptos)
            {
                Cryptocurrencies.Add(crypto);
            }

            _logger.LogInformation("Loaded {Count} cryptocurrencies", cryptos.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Load prices operation was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error loading crypto prices");
            ErrorMessage = "Network error. Please check your connection.";
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while loading prices");
            ErrorMessage = "Session expired. Please log in again.";
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading crypto prices");
            ErrorMessage = "Failed to load prices. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes cryptocurrency prices (pull-to-refresh).
    /// </summary>
    [RelayCommand]
    private async Task RefreshPricesAsync()
    {
        if (IsRefreshing) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsRefreshing = true;
        ErrorMessage = null;

        try
        {
            _logger.LogInformation("Refreshing cryptocurrency prices");
            var cryptos = await _cryptoService.GetCryptoPricesAsync(_cts.Token);

            Cryptocurrencies.Clear();
            foreach (var crypto in cryptos)
            {
                Cryptocurrencies.Add(crypto);
            }

            _logger.LogInformation("Refreshed {Count} cryptocurrencies", cryptos.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Refresh operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing crypto prices");
            ErrorMessage = "Failed to refresh. Pull down to try again.";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Navigates to the purchase page for the selected cryptocurrency.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToPurchaseAsync()
    {
        if (SelectedCrypto == null)
        {
            _logger.LogWarning("No cryptocurrency selected for purchase navigation");
            return;
        }

        _logger.LogInformation("Navigating to purchase page for {Symbol}", SelectedCrypto.Symbol);
        await Shell.Current.GoToAsync($"//PurchasePage?symbol={SelectedCrypto.Symbol}");
    }

    /// <summary>
    /// Navigates to view details for a specific cryptocurrency.
    /// </summary>
    [RelayCommand]
    private async Task ViewCryptoDetailsAsync(CryptoCurrency crypto)
    {
        if (crypto == null) return;

        _logger.LogInformation("Viewing details for {Symbol}", crypto.Symbol);
        SelectedCrypto = crypto;
        // Could navigate to a details page here
        await Shell.Current.DisplayAlertAsync(
            crypto.Name,
            $"Price: {crypto.FormattedPrice}\nChange: {crypto.FormattedPercentChange}\nMarket Cap: ${crypto.MarketCap:N0}",
            "OK");
    }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        _logger.LogDebug("Dashboard page disappearing, operations cancelled");
    }
}
