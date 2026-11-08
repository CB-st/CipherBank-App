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
public partial class PurchaseViewModel(
    ILogger<PurchaseViewModel> logger,
    ICryptoApiService cryptoService,
    ITransactionService transactionService,
    IErrorHandler errorHandler,
    INavigationService navigation,
    IDialogService dialog)
    : ObservableObject, IQueryAttributable, IDisposable
{
    private const decimal FeePercentage = 0.015m; // 1.5% fee

    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>Gets or sets the cryptocurrencies available to purchase.</summary>
    [ObservableProperty]
    public partial ObservableCollection<CryptoCurrency> AvailableCryptos { get; set; } = [];

    /// <summary>Gets or sets the selected cryptocurrency.</summary>
    [ObservableProperty]
    public partial CryptoCurrency? SelectedCrypto { get; set; }

    /// <summary>Gets or sets the focused cryptocurrency card.</summary>
    [ObservableProperty]
    public partial CryptoCurrency? FocusedCrypto { get; set; }

    /// <summary>Gets or sets the optional payment note.</summary>
    [ObservableProperty]
    public partial string PaymentNote { get; set; } = string.Empty;

    /// <summary>Gets or sets the purchase amount.</summary>
    [ObservableProperty]
    public partial decimal Amount { get; set; }

    /// <summary>Gets or sets the total purchase cost.</summary>
    [ObservableProperty]
    public partial decimal TotalCost { get; set; }

    /// <summary>Gets or sets the purchase fee.</summary>
    [ObservableProperty]
    public partial decimal Fee { get; set; }

    /// <summary>Gets or sets a value indicating whether a purchase is in progress.</summary>
    [ObservableProperty]
    public partial bool IsPurchasing { get; set; }

    /// <summary>Gets or sets a value indicating whether purchase data is loading.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Gets or sets the purchase error message.</summary>
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the amount text entered by the user.</summary>
    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    /// <summary>
    /// Handles query parameters passed to this page.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("symbol", out object? symbolObj) || symbolObj is not string symbol)
        {
            return;
        }

        LogReceivedSymbol(logger, symbol);
        _ = SelectCryptoBySymbolAsync(symbol);
    }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        LogPurchaseDisappearing(logger);
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
        if (decimal.TryParse(value, out decimal parsed) && parsed >= 0)
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
        if (!AssetSymbol.TryParse(symbol, out AssetSymbol? parsedSymbol))
        {
            return;
        }

        CryptoCurrency? crypto = AvailableCryptos.FirstOrDefault(c => c.Symbol == parsedSymbol);

        if (crypto != null)
        {
            SelectedCrypto = crypto;
            LogPreSelectedSymbol(logger, parsedSymbol.Value);
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
            LogLoadingCryptos(logger);

            bool success = await errorHandler.HandleApiErrorsAsync(
                async () =>
                {
                    List<CryptoCurrency> cryptos = await cryptoService.GetCryptoPricesAsync(_cts.Token);

                    AvailableCryptos.Clear();
                    foreach (CryptoCurrency crypto in cryptos)
                    {
                        AvailableCryptos.Add(crypto);
                    }

                    if (AvailableCryptos.Count > 0)
                    {
                        // Reloaded records are new instances; re-resolve the selection by
                        // symbol so the deck recenters on the refreshed item.
                        CryptoCurrency? restored = SelectedCrypto != null
                            ? AvailableCryptos.FirstOrDefault(c => c.Symbol == SelectedCrypto.Symbol)
                            : null;
                        SelectedCrypto = restored ?? AvailableCryptos.First();
                    }

                    LogLoadedCryptos(logger, cryptos.Count);
                },
                msg => ErrorMessage = msg);

            if (!success)
            {
                if (ErrorMessage == null)
                {
                    LogLoadCryptosCancelled(logger);
                }
            }
        }
        catch (Exception ex)
        {
            LogUnexpectedErrorLoadingCryptos(logger, ex);
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

        decimal subtotal = Amount * SelectedCrypto.CurrentPrice;
        Fee = subtotal * FeePercentage;
        TotalCost = subtotal + Fee;

        LogCalculatedPurchase(logger, Amount, SelectedCrypto.Symbol.Value, subtotal, Fee, TotalCost);
    }

    /// <summary>
    /// Purchases the selected cryptocurrency.
    /// </summary>
    [RelayCommand]
    private async Task PurchaseCryptoAsync()
    {
        if (SelectedCrypto == null)
        {
            await dialog.ShowAlertAsync("Error", "Please select a cryptocurrency.");
            return;
        }

        if (Amount <= 0)
        {
            await dialog.ShowAlertAsync("Error", "Please enter a valid amount.");
            return;
        }

        // Confirm purchase
        string confirmMessage =
            $"Buy {Amount:F8} {SelectedCrypto.Symbol} ({SelectedCrypto.Name})\n\n" +
            $"Subtotal: ${Amount * SelectedCrypto.CurrentPrice:F2}\n" +
            $"Fee (1.5%): ${Fee:F2}\n" +
            $"Total: ${TotalCost:F2}";

        if (!string.IsNullOrWhiteSpace(PaymentNote))
        {
            confirmMessage += $"\nNote: {PaymentNote}";
        }

        bool confirm = await dialog.ShowConfirmAsync(
            "Confirm Purchase",
            confirmMessage,
            "Purchase");

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
            LogPurchasing(logger, Amount, SelectedCrypto.Symbol.Value);

            Transaction transaction = await transactionService.PurchaseCryptoAsync(
                SelectedCrypto.Symbol, Amount, _cts.Token);

            string successMessage =
                $"Successfully purchased {transaction.Amount:F8} {transaction.CryptoSymbol}!\n\n" +
                $"Transaction ID: {transaction.Id}\n" +
                $"Fee: {transaction.FeeAmount:F8} {transaction.CryptoSymbol}";
            await dialog.ShowAlertAsync(
                "Purchase Complete",
                successMessage);

            // Clear form
            AmountText = string.Empty;
            Amount = 0;
            PaymentNote = string.Empty;
            CalculateTotalCost();

            LogPurchaseCompleted(logger, transaction.Id);

            // Optionally navigate to wallet
            bool viewWallet = await dialog.ShowConfirmAsync(
                "View Wallet?",
                "Would you like to view your wallet?",
                "Yes",
                "No");

            if (viewWallet)
            {
                await navigation.GoToAsync(Routes.Wallet);
            }
        }
        catch (InvalidOperationException ex)
        {
            LogPurchaseFailed(logger, ex, ex.Message);
            await dialog.ShowAlertAsync("Purchase Failed", ex.Message);
        }
        catch (ArgumentException ex)
        {
            LogInvalidPurchaseParameters(logger, ex);
            await dialog.ShowAlertAsync("Invalid Input", ex.Message);
        }
        catch (OperationCanceledException)
        {
            LogPurchaseCancelled(logger);
        }
        catch (Exception ex)
        {
            LogErrorProcessingPurchase(logger, ex);
            await dialog.ShowAlertAsync(
                "Error",
                "Failed to complete purchase. Please try again.");
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
        if (SelectedCrypto == null || !decimal.TryParse(usdAmountString, out decimal usdAmount))
        {
            return;
        }

        Amount = usdAmount / SelectedCrypto.CurrentPrice;
        AmountText = Amount.ToString("F8", CultureInfo.CurrentCulture);
        CalculateTotalCost();

        LogSetPresetAmount(logger, usdAmount, Amount, SelectedCrypto.Symbol.Value);
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
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _cts?.Dispose();
        }

        _disposed = true;
    }
}
