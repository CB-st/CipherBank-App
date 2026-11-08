// <copyright file="DashboardViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.ObjectModel;
using CipherBank_app.Constants;
using CipherBank_app.Models;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page displaying cryptocurrency prices and market data.
/// </summary>
public partial class DashboardViewModel(
    ILogger<DashboardViewModel> logger,
    ICryptoApiService cryptoService,
    IErrorHandler errorHandler,
    INavigationService navigation,
    IDialogService dialog)
    : ObservableObject, IDisposable
{
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>Gets or sets the cryptocurrencies shown on the dashboard.</summary>
    [ObservableProperty]
    public partial ObservableCollection<CryptoCurrency> Cryptocurrencies { get; set; } = [];

    /// <summary>Gets or sets the selected cryptocurrency.</summary>
    [ObservableProperty]
    public partial CryptoCurrency? SelectedCrypto { get; set; }

    /// <summary>Gets or sets a value indicating whether prices are loading.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Gets or sets a value indicating whether a refresh is in progress.</summary>
    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    /// <summary>Gets or sets the dashboard error message.</summary>
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the total portfolio value.</summary>
    [ObservableProperty]
    public partial decimal TotalPortfolioValue { get; set; }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        LogDashboardDisappearing(logger);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Loads cryptocurrency prices from the API.
    /// </summary>
    [RelayCommand]
    private async Task LoadPricesAsync() => await LoadCryptoPricesAsync(isRefresh: false);

    /// <summary>
    /// Refreshes cryptocurrency prices (pull-to-refresh).
    /// </summary>
    [RelayCommand]
    private async Task RefreshPricesAsync() => await LoadCryptoPricesAsync(isRefresh: true);

    private async Task LoadCryptoPricesAsync(bool isRefresh)
    {
        if (isRefresh ? IsRefreshing : IsLoading)
        {
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        if (isRefresh)
        {
            IsRefreshing = true;
        }
        else
        {
            IsLoading = true;
        }

        ErrorMessage = null;

        try
        {
            if (isRefresh)
            {
                LogRefreshingPrices(logger);
            }
            else
            {
                LogLoadingPrices(logger);
            }

            var success = await errorHandler.HandleApiErrorsAsync(
                async () =>
                {
                    List<CryptoCurrency> cryptos = await cryptoService.GetCryptoPricesAsync(_cts.Token);
                    Cryptocurrencies.Clear();
                    foreach (CryptoCurrency crypto in cryptos)
                    {
                        Cryptocurrencies.Add(crypto);
                    }

                    if (isRefresh)
                    {
                        LogRefreshedCount(logger, cryptos.Count);
                    }
                    else
                    {
                        LogLoadedCount(logger, cryptos.Count);
                    }
                },
                msg => ErrorMessage = msg,
                isRefresh ? "Failed to refresh. Pull down to try again." : "Network error. Please check your connection.");

            if (!success)
            {
                if (ErrorMessage == null)
                {
                    if (isRefresh)
                    {
                        LogRefreshCancelled(logger);
                    }
                    else
                    {
                        LogLoadPricesCancelled(logger);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogErrorLoadingPrices(logger, ex);
            ErrorMessage = isRefresh ? "Failed to refresh. Pull down to try again." : "Failed to load prices. Please try again.";
        }
        finally
        {
            if (isRefresh)
            {
                IsRefreshing = false;
            }
            else
            {
                IsLoading = false;
            }
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
            LogNoCryptoSelected(logger);
            return;
        }

        LogNavigatingToPurchase(logger, SelectedCrypto.Symbol.Value);
        await navigation.GoToAsync(Routes.PurchaseWithSymbol(SelectedCrypto.Symbol));
    }

    /// <summary>
    /// Navigates to the wallets page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToWalletsAsync()
    {
        LogNavigatingToWallets(logger);
        await navigation.GoToAsync(Routes.Wallet);
    }

    /// <summary>
    /// Navigates to view details for a specific cryptocurrency.
    /// </summary>
    [RelayCommand]
    private async Task ViewCryptoDetailsAsync(CryptoCurrency crypto)
    {
        LogViewingDetails(logger, crypto.Symbol.Value);
        SelectedCrypto = crypto;

        // Could navigate to a details page here
        var detailMessage = $"Price: {crypto.FormattedPrice}\nChange: {crypto.FormattedPercentChange}\nMarket Cap: ${crypto.MarketCap:N0}";
        await dialog.ShowAlertAsync(
            crypto.Name,
            detailMessage);
    }

#pragma warning disable SA1204 // Static members should appear before non-static members - LoggerMessage source generators
    [LoggerMessage(Level = LogLevel.Information, Message = "Refreshing cryptocurrency prices")]
    private static partial void LogRefreshingPrices(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading cryptocurrency prices")]
    private static partial void LogLoadingPrices(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Refreshed {Count} cryptocurrencies")]
    private static partial void LogRefreshedCount(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {Count} cryptocurrencies")]
    private static partial void LogLoadedCount(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refresh operation was cancelled")]
    private static partial void LogRefreshCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Load prices operation was cancelled")]
    private static partial void LogLoadPricesCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading crypto prices")]
    private static partial void LogErrorLoadingPrices(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No cryptocurrency selected for purchase navigation")]
    private static partial void LogNoCryptoSelected(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Navigating to purchase page for {Symbol}")]
    private static partial void LogNavigatingToPurchase(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Navigating to wallets page")]
    private static partial void LogNavigatingToWallets(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Viewing details for {Symbol}")]
    private static partial void LogViewingDetails(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dashboard page disappearing, operations cancelled")]
    private static partial void LogDashboardDisappearing(ILogger logger);
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
