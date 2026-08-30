// <copyright file="PurchaseViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.ObjectModel;
using System.Globalization;
using CipherBank_app.Constants;
using CipherBank_app.Models;
using CipherBank_app.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.ViewModels;

/// <summary>
/// ViewModel for the Purchase page allowing users to buy cryptocurrency.
/// </summary>
public partial class PurchaseViewModel : ObservableObject, IQueryAttributable, IDisposable
{
    private const decimal FeePercentage = 0.015m; // 1.5% fee

    private readonly ILogger<PurchaseViewModel> _logger;
    private readonly ICryptoApiService _cryptoService;
    private readonly ITransactionService _transactionService;
    private readonly IErrorHandler _errorHandler;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    [ObservableProperty]
    private ObservableCollection<CryptoCurrency> availableCryptos = [];

    [ObservableProperty]
    private CryptoCurrency? selectedCrypto;

    [ObservableProperty]
    private CryptoCurrency? focusedCrypto;

    [ObservableProperty]
    private string paymentNote = string.Empty;

    [ObservableProperty]
    private decimal amount;

    [ObservableProperty]
    private decimal totalCost;

    [ObservableProperty]
    private decimal fee;

    [ObservableProperty]
    private bool isPurchasing;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string amountText = string.Empty;

    public PurchaseViewModel(
        ILogger<PurchaseViewModel> logger,
        ICryptoApiService cryptoService,
        ITransactionService transactionService,
        IErrorHandler errorHandler,
        INavigationService navigation,
        IDialogService dialog)
    {
        _logger = logger;
        _cryptoService = cryptoService;
        _transactionService = transactionService;
        _errorHandler = errorHandler;
        _navigation = navigation;
        _dialog = dialog;
    }

    /// <summary>
    /// Handles query parameters passed to this page.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("symbol", out var symbolObj) && symbolObj is string symbol)
        {
            LogReceivedSymbol(_logger, symbol);
            _ = SelectCryptoBySymbolAsync(symbol);
        }
    }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        LogPurchaseDisappearing(_logger);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    partial void OnSelectedCryptoChanged(CryptoCurrency? value)
    {
        CalculateTotalCost();

        if (value != null && !Equals(FocusedCrypto, value))
        {
            FocusedCrypto = value;
        }
    }

    partial void OnFocusedCryptoChanged(CryptoCurrency? value)
    {
        if (value != null && !Equals(SelectedCrypto, value))
        {
            SelectedCrypto = value;
        }
    }

    partial void OnAmountTextChanged(string value)
    {
        if (decimal.TryParse(value, out var parsed) && parsed >= 0)
        {
            Amount = parsed;
            CalculateTotalCost();
        }
        else if (string.IsNullOrWhiteSpace(value))
        {
            Amount = 0;
            CalculateTotalCost();
        }
    }

    private async Task SelectCryptoBySymbolAsync(string symbol)
    {
        await LoadAvailableCryptosAsync();

        var crypto = AvailableCryptos.FirstOrDefault(c =>
            c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

        if (crypto != null)
        {
            SelectedCrypto = crypto;
            LogPreSelectedSymbol(_logger, symbol);
        }
    }

    /// <summary>
    /// Loads available cryptocurrencies for purchase.
    /// </summary>
    [RelayCommand]
    private async Task LoadAvailableCryptosAsync()
    {
        if (IsLoading)
        {
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            LogLoadingCryptos(_logger);

            var success = await _errorHandler.HandleApiErrorsAsync(
                async () =>
                {
                    var cryptos = await _cryptoService.GetCryptoPricesAsync(_cts.Token);

                    AvailableCryptos.Clear();
                    foreach (var crypto in cryptos)
                    {
                        AvailableCryptos.Add(crypto);
                    }

                    if (AvailableCryptos.Count > 0)
                    {
                        // Reloaded records are new instances; re-resolve the selection by
                        // symbol so the deck recenters on the refreshed item.
                        var restored = SelectedCrypto != null
                            ? AvailableCryptos.FirstOrDefault(c => c.Symbol == SelectedCrypto.Symbol)
                            : null;
                        SelectedCrypto = restored ?? AvailableCryptos.First();
                    }

                    LogLoadedCryptos(_logger, cryptos.Count);
                },
                msg => ErrorMessage = msg);

            if (!success)
            {
                if (ErrorMessage == null)
                {
                    LogLoadCryptosCancelled(_logger);
                }

                return;
            }
        }
        catch (Exception ex)
        {
            LogUnexpectedErrorLoadingCryptos(_logger, ex);
            ErrorMessage = "Failed to load cryptocurrencies. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Calculates the total cost including fees.
    /// </summary>
    [RelayCommand]
    private void CalculateTotalCost()
    {
        if (SelectedCrypto == null || Amount <= 0)
        {
            TotalCost = 0;
            Fee = 0;
            return;
        }

        var subtotal = Amount * SelectedCrypto.CurrentPrice;
        Fee = subtotal * FeePercentage;
        TotalCost = subtotal + Fee;

        LogCalculatedPurchase(_logger, Amount, SelectedCrypto.Symbol, subtotal, Fee, TotalCost);
    }

    /// <summary>
    /// Purchases the selected cryptocurrency.
    /// </summary>
    [RelayCommand]
    private async Task PurchaseCryptoAsync()
    {
        if (SelectedCrypto == null)
        {
            await _dialog.ShowAlertAsync("Error", "Please select a cryptocurrency.", "OK");
            return;
        }

        if (Amount <= 0)
        {
            await _dialog.ShowAlertAsync("Error", "Please enter a valid amount.", "OK");
            return;
        }

        // Confirm purchase
        var confirmMessage =
            $"Buy {Amount:F8} {SelectedCrypto.Symbol} ({SelectedCrypto.Name})\n\n" +
            $"Subtotal: ${Amount * SelectedCrypto.CurrentPrice:F2}\n" +
            $"Fee (1.5%): ${Fee:F2}\n" +
            $"Total: ${TotalCost:F2}";

        if (!string.IsNullOrWhiteSpace(PaymentNote))
        {
            confirmMessage += $"\nNote: {PaymentNote}";
        }

        var confirm = await _dialog.ShowConfirmAsync(
            "Confirm Purchase",
            confirmMessage,
            "Purchase",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsPurchasing = true;
        ErrorMessage = null;

        try
        {
            LogPurchasing(_logger, Amount, SelectedCrypto.Symbol);

            var transaction = await _transactionService.PurchaseCryptoAsync(
                SelectedCrypto.Symbol, Amount, _cts.Token);

            var successMessage =
                $"Successfully purchased {transaction.Amount:F8} {transaction.CryptoSymbol}!\n\n" +
                $"Transaction ID: {transaction.Id}\n" +
                $"Fee: {transaction.FeeAmount:F8} {transaction.CryptoSymbol}";
            await _dialog.ShowAlertAsync(
                "Purchase Complete",
                successMessage,
                "OK");

            // Clear form
            AmountText = string.Empty;
            Amount = 0;
            PaymentNote = string.Empty;
            CalculateTotalCost();

            LogPurchaseCompleted(_logger, transaction.Id);

            // Optionally navigate to wallet
            var viewWallet = await _dialog.ShowConfirmAsync(
                "View Wallet?",
                "Would you like to view your wallet?",
                "Yes",
                "No");

            if (viewWallet)
            {
                await _navigation.GoToAsync(Routes.Wallet);
            }
        }
        catch (InvalidOperationException ex)
        {
            LogPurchaseFailed(_logger, ex, ex.Message);
            await _dialog.ShowAlertAsync("Purchase Failed", ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            LogInvalidPurchaseParameters(_logger, ex);
            await _dialog.ShowAlertAsync("Invalid Input", ex.Message, "OK");
        }
        catch (OperationCanceledException)
        {
            LogPurchaseCancelled(_logger);
        }
        catch (Exception ex)
        {
            LogErrorProcessingPurchase(_logger, ex);
            await _dialog.ShowAlertAsync(
                "Error",
                "Failed to complete purchase. Please try again.",
                "OK");
        }
        finally
        {
            IsPurchasing = false;
        }
    }

    /// <summary>
    /// Sets a preset amount based on USD value.
    /// </summary>
    [RelayCommand]
    private void SetPresetAmount(string usdAmountString)
    {
        if (SelectedCrypto == null || !decimal.TryParse(usdAmountString, out var usdAmount))
        {
            return;
        }

        Amount = usdAmount / SelectedCrypto.CurrentPrice;
        AmountText = Amount.ToString("F8", CultureInfo.CurrentCulture);
        CalculateTotalCost();

        LogSetPresetAmount(_logger, usdAmount, Amount, SelectedCrypto.Symbol);
    }

#pragma warning disable SA1204 // Static members should appear before non-static members - LoggerMessage source generators
    [LoggerMessage(Level = LogLevel.Information, Message = "Received symbol parameter: {Symbol}")]
    private static partial void LogReceivedSymbol(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pre-selected {Symbol} for purchase")]
    private static partial void LogPreSelectedSymbol(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading available cryptocurrencies for purchase")]
    private static partial void LogLoadingCryptos(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {Count} cryptocurrencies for purchase")]
    private static partial void LogLoadedCryptos(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Load cryptos operation was cancelled")]
    private static partial void LogLoadCryptosCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error loading cryptocurrencies")]
    private static partial void LogUnexpectedErrorLoadingCryptos(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Calculated purchase: {Amount} {Symbol} = ${Subtotal:F2} + ${Fee:F2} fee = ${Total:F2}")]
    private static partial void LogCalculatedPurchase(ILogger logger, decimal amount, string symbol, decimal subtotal, decimal fee, decimal total);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purchasing {Amount} {Symbol}")]
    private static partial void LogPurchasing(ILogger logger, decimal amount, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purchase completed. Transaction ID: {TransactionId}")]
    private static partial void LogPurchaseCompleted(ILogger logger, string transactionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Purchase failed: {Message}")]
    private static partial void LogPurchaseFailed(ILogger logger, Exception ex, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid purchase parameters")]
    private static partial void LogInvalidPurchaseParameters(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Purchase operation was cancelled")]
    private static partial void LogPurchaseCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing purchase")]
    private static partial void LogErrorProcessingPurchase(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Set preset amount: ${Usd} = {Amount} {Symbol}")]
    private static partial void LogSetPresetAmount(ILogger logger, decimal usd, decimal amount, string symbol);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Purchase page disappearing, operations cancelled")]
    private static partial void LogPurchaseDisappearing(ILogger logger);
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
