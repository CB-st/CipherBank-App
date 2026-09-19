// <copyright file="DashboardViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Constants;
using CipherBank_app.Models;
using CipherBank_app.Services;
using CipherBank_app.V1;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

/// <summary>
/// ViewModel for the Dashboard page displaying cryptocurrency prices and market data.
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly IProductClient _product;
    private readonly IErrorHandler _errorHandler;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
    private CancellationTokenSource? _cts;
    private bool _disposed;

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

    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        IProductClient product,
        IErrorHandler errorHandler,
        INavigationService navigation,
        IDialogService dialog)
    {
        _logger = logger;
        _product = product;
        _errorHandler = errorHandler;
        _navigation = navigation;
        _dialog = dialog;
    }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        LogDashboardDisappearing(_logger);
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
                LogRefreshingPrices(_logger);
            }
            else
            {
                LogLoadingPrices(_logger);
            }

            var success = await _errorHandler.HandleApiErrorsAsync(
                async () =>
                {
                    PortfolioDto portfolio = await _product.GetPortfolioAsync(_cts.Token);
                    Cryptocurrencies.Clear();
                    foreach (HoldingDto holding in portfolio.Holdings)
                    {
                        Cryptocurrencies.Add(ProductSurfaceMap.ToCryptoCurrency(holding));
                    }

                    if (isRefresh)
                    {
                        LogRefreshedCount(_logger, portfolio.Holdings.Count);
                    }
                    else
                    {
                        LogLoadedCount(_logger, portfolio.Holdings.Count);
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
                        LogRefreshCancelled(_logger);
                    }
                    else
                    {
                        LogLoadPricesCancelled(_logger);
                    }
                }

                return;
            }
        }
        catch (Exception ex)
        {
            LogErrorLoadingPrices(_logger, ex);
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
            LogNoCryptoSelected(_logger);
            return;
        }

        LogNavigatingToPurchase(_logger, SelectedCrypto.Symbol);
        await _navigation.GoToAsync(Routes.PurchaseWithSymbol(SelectedCrypto.Symbol));
    }

    /// <summary>
    /// Navigates to the wallets page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToWalletsAsync()
    {
        LogNavigatingToWallets(_logger);
        await _navigation.GoToAsync(Routes.Wallet);
    }

    /// <summary>
    /// Navigates to view details for a specific cryptocurrency.
    /// </summary>
    [RelayCommand]
    private async Task ViewCryptoDetailsAsync(CryptoCurrency crypto)
    {
        if (crypto == null)
        {
            return;
        }

        LogViewingDetails(_logger, crypto.Symbol);
        SelectedCrypto = crypto;

        // Could navigate to a details page here
        var detailMessage = $"Price: {crypto.FormattedPrice}\nChange: {crypto.FormattedPercentChange}\nMarket Cap: ${crypto.MarketCap:N0}";
        await _dialog.ShowAlertAsync(
            crypto.Name,
            detailMessage,
            "OK");
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
