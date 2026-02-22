using System;
using System.Collections.ObjectModel;
using System.Linq;
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
/// ViewModel for the Purchase page allowing users to buy cryptocurrency.
/// </summary>
public partial class PurchaseViewModel : ObservableObject, IQueryAttributable
{
    private readonly ILogger<PurchaseViewModel> _logger;
    private readonly ICryptoApiService _cryptoService;
    private readonly ITransactionService _transactionService;
    private CancellationTokenSource? _cts;

    private const decimal FeePercentage = 0.015m; // 1.5% fee

    public PurchaseViewModel(
        ILogger<PurchaseViewModel> logger,
        ICryptoApiService cryptoService,
        ITransactionService transactionService)
    {
        _logger = logger;
        _cryptoService = cryptoService;
        _transactionService = transactionService;
    }

    [ObservableProperty]
    private ObservableCollection<CryptoCurrency> availableCryptos = [];

    [ObservableProperty]
    private CryptoCurrency? selectedCrypto;

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

    partial void OnSelectedCryptoChanged(CryptoCurrency? value)
    {
        CalculateTotalCost();
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

    /// <summary>
    /// Handles query parameters passed to this page.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("symbol", out var symbolObj) && symbolObj is string symbol)
        {
            _logger.LogInformation("Received symbol parameter: {Symbol}", symbol);
            _ = SelectCryptoBySymbolAsync(symbol);
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
            _logger.LogInformation("Pre-selected {Symbol} for purchase", symbol);
        }
    }

    /// <summary>
    /// Loads available cryptocurrencies for purchase.
    /// </summary>
    [RelayCommand]
    private async Task LoadAvailableCryptosAsync()
    {
        if (IsLoading) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _logger.LogInformation("Loading available cryptocurrencies for purchase");
            var cryptos = await _cryptoService.GetCryptoPricesAsync(_cts.Token);

            AvailableCryptos.Clear();
            foreach (var crypto in cryptos)
            {
                AvailableCryptos.Add(crypto);
            }

            if (AvailableCryptos.Count > 0 && SelectedCrypto == null)
            {
                SelectedCrypto = AvailableCryptos.First();
            }

            _logger.LogInformation("Loaded {Count} cryptocurrencies for purchase", cryptos.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Load cryptos operation was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error loading cryptocurrencies");
            ErrorMessage = "Network error. Please check your connection.";
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while loading cryptocurrencies");
            ErrorMessage = "Session expired. Please log in again.";
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading cryptocurrencies");
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

        _logger.LogDebug("Calculated purchase: {Amount} {Symbol} = ${Subtotal:F2} + ${Fee:F2} fee = ${Total:F2}",
            Amount, SelectedCrypto.Symbol, subtotal, Fee, TotalCost);
    }

    /// <summary>
    /// Purchases the selected cryptocurrency.
    /// </summary>
    [RelayCommand]
    private async Task PurchaseCryptoAsync()
    {
        if (SelectedCrypto == null)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please select a cryptocurrency.", "OK");
            return;
        }

        if (Amount <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please enter a valid amount.", "OK");
            return;
        }

        // Confirm purchase
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Confirm Purchase",
            $"Buy {Amount:F8} {SelectedCrypto.Symbol} ({SelectedCrypto.Name})\n\n" +
            $"Subtotal: ${Amount * SelectedCrypto.CurrentPrice:F2}\n" +
            $"Fee (1.5%): ${Fee:F2}\n" +
            $"Total: ${TotalCost:F2}",
            "Purchase", "Cancel");

        if (!confirm) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsPurchasing = true;
        ErrorMessage = null;

        try
        {
            _logger.LogInformation("Purchasing {Amount} {Symbol}", Amount, SelectedCrypto.Symbol);

            var transaction = await _transactionService.PurchaseCryptoAsync(
                SelectedCrypto.Symbol, Amount, _cts.Token);

            await Shell.Current.DisplayAlertAsync(
                "Purchase Complete",
                $"Successfully purchased {transaction.Amount:F8} {transaction.CryptoSymbol}!\n\n" +
                $"Transaction ID: {transaction.Id}\n" +
                $"Fee: {transaction.FeeAmount:F8} {transaction.CryptoSymbol}",
                "OK");

            // Clear form
            AmountText = string.Empty;
            Amount = 0;
            CalculateTotalCost();

            _logger.LogInformation("Purchase completed. Transaction ID: {TransactionId}", transaction.Id);

            // Optionally navigate to wallet
            var viewWallet = await Shell.Current.DisplayAlertAsync(
                "View Wallet?",
                "Would you like to view your wallet?",
                "Yes", "No");

            if (viewWallet)
            {
                await Shell.Current.GoToAsync("//WalletPage");
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Purchase failed: {Message}", ex.Message);
            await Shell.Current.DisplayAlertAsync("Purchase Failed", ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid purchase parameters");
            await Shell.Current.DisplayAlertAsync("Invalid Input", ex.Message, "OK");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Purchase operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing purchase");
            await Shell.Current.DisplayAlertAsync("Error",
                "Failed to complete purchase. Please try again.", "OK");
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
            return;

        Amount = usdAmount / SelectedCrypto.CurrentPrice;
        AmountText = Amount.ToString("F8");
        CalculateTotalCost();

        _logger.LogDebug("Set preset amount: ${USD} = {Amount} {Symbol}",
            usdAmount, Amount, SelectedCrypto.Symbol);
    }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        _logger.LogDebug("Purchase page disappearing, operations cancelled");
    }
}
