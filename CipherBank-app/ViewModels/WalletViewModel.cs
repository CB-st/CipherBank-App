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
/// ViewModel for the Wallet page displaying user's cryptocurrency wallets and transactions.
/// </summary>
public partial class WalletViewModel : ObservableObject
{
    private readonly ILogger<WalletViewModel> _logger;
    private readonly IWalletService _walletService;
    private readonly ITransactionService _transactionService;
    private readonly ICryptoApiService _cryptoService;
    private CancellationTokenSource? _cts;

    public WalletViewModel(
        ILogger<WalletViewModel> logger,
        IWalletService walletService,
        ITransactionService transactionService,
        ICryptoApiService cryptoService)
    {
        _logger = logger;
        _walletService = walletService;
        _transactionService = transactionService;
        _cryptoService = cryptoService;
    }

    [ObservableProperty]
    private ObservableCollection<Wallet> wallets = [];

    [ObservableProperty]
    private ObservableCollection<Transaction> transactions = [];

    [ObservableProperty]
    private Wallet? selectedWallet;

    [ObservableProperty]
    private decimal totalBalance;

    [ObservableProperty]
    private decimal totalBalanceUsd;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isLoadingTransactions;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private string sendToAddress = string.Empty;

    [ObservableProperty]
    private decimal sendAmount;

    [ObservableProperty]
    private bool isSending;

    partial void OnSelectedWalletChanged(Wallet? value)
    {
        if (value != null)
        {
            _ = LoadTransactionsAsync();
        }
    }

    /// <summary>
    /// Loads all user wallets and calculates total balance.
    /// </summary>
    [RelayCommand]
    private async Task LoadWalletsAsync()
    {
        if (IsLoading) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _logger.LogInformation("Loading wallets");
            var walletList = await _walletService.GetWalletsAsync(_cts.Token);

            Wallets.Clear();
            decimal totalUsd = 0;

            foreach (var wallet in walletList)
            {
                Wallets.Add(wallet);

                // Calculate USD value for each wallet
                try
                {
                    var crypto = await _cryptoService.GetCryptoPriceAsync(wallet.CryptoSymbol, _cts.Token);
                    totalUsd += wallet.Balance * crypto.CurrentPrice;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not get price for {Symbol}", wallet.CryptoSymbol);
                }
            }

            TotalBalanceUsd = totalUsd;

            if (Wallets.Count > 0 && SelectedWallet == null)
            {
                SelectedWallet = Wallets.First();
            }

            _logger.LogInformation("Loaded {Count} wallets with total value ${Value:N2}",
                walletList.Count, TotalBalanceUsd);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Load wallets operation was cancelled");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error loading wallets");
            ErrorMessage = "Network error. Please check your connection.";
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while loading wallets");
            ErrorMessage = "Session expired. Please log in again.";
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading wallets");
            ErrorMessage = "Failed to load wallets. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads transaction history for the selected wallet.
    /// </summary>
    [RelayCommand]
    private async Task LoadTransactionsAsync()
    {
        if (SelectedWallet == null || IsLoadingTransactions) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsLoadingTransactions = true;

        try
        {
            _logger.LogInformation("Loading transactions for wallet {WalletId}", SelectedWallet.Id);
            var txList = await _transactionService.GetTransactionHistoryAsync(
                SelectedWallet.Id, _cts.Token);

            Transactions.Clear();
            foreach (var tx in txList)
            {
                Transactions.Add(tx);
            }

            _logger.LogInformation("Loaded {Count} transactions", txList.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Load transactions operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading transactions");
        }
        finally
        {
            IsLoadingTransactions = false;
        }
    }

    /// <summary>
    /// Sends cryptocurrency to another address.
    /// </summary>
    [RelayCommand]
    private async Task SendCryptoAsync()
    {
        if (SelectedWallet == null)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please select a wallet first.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(SendToAddress))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please enter a destination address.", "OK");
            return;
        }

        if (SendAmount <= 0)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please enter a valid amount.", "OK");
            return;
        }

        if (SendAmount > SelectedWallet.Balance)
        {
            await Shell.Current.DisplayAlertAsync("Error",
                $"Insufficient balance. Available: {SelectedWallet.FormattedBalance}", "OK");
            return;
        }

        // Confirm transaction
        var confirm = await Shell.Current.DisplayAlertAsync(
            "Confirm Send",
            $"Send {SendAmount:F8} {SelectedWallet.CryptoSymbol} to:\n{SendToAddress}",
            "Send", "Cancel");

        if (!confirm) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsSending = true;
        ErrorMessage = null;

        try
        {
            _logger.LogInformation("Sending {Amount} {Symbol} to {Address}",
                SendAmount, SelectedWallet.CryptoSymbol, SendToAddress);

            var transaction = await _transactionService.SendCryptoAsync(
                SelectedWallet.Id, SendToAddress, SendAmount, _cts.Token);

            await Shell.Current.DisplayAlertAsync(
                "Success",
                $"Transaction submitted!\nID: {transaction.Id}\nStatus: {transaction.Status}",
                "OK");

            // Clear form and refresh
            SendToAddress = string.Empty;
            SendAmount = 0;
            await LoadWalletsAsync();
            await LoadTransactionsAsync();

            _logger.LogInformation("Send completed. Transaction ID: {TransactionId}", transaction.Id);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Send failed: {Message}", ex.Message);
            await Shell.Current.DisplayAlertAsync("Transaction Failed", ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid send parameters");
            await Shell.Current.DisplayAlertAsync("Invalid Input", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending crypto");
            await Shell.Current.DisplayAlertAsync("Error",
                "Failed to send transaction. Please try again.", "OK");
        }
        finally
        {
            IsSending = false;
        }
    }

    /// <summary>
    /// Creates a new wallet for a cryptocurrency.
    /// </summary>
    [RelayCommand]
    private async Task CreateWalletAsync(string cryptoSymbol)
    {
        if (string.IsNullOrWhiteSpace(cryptoSymbol)) return;

        try
        {
            _logger.LogInformation("Creating wallet for {Symbol}", cryptoSymbol);
            var wallet = await _walletService.CreateWalletAsync(cryptoSymbol);

            Wallets.Add(wallet);
            SelectedWallet = wallet;

            await Shell.Current.DisplayAlertAsync(
                "Wallet Created",
                $"New {wallet.CryptoName} wallet created!\nAddress: {wallet.Address}",
                "OK");

            _logger.LogInformation("Created wallet {WalletId} for {Symbol}", wallet.Id, cryptoSymbol);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Could not create wallet: {Message}", ex.Message);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wallet");
            await Shell.Current.DisplayAlertAsync("Error",
                "Failed to create wallet. Please try again.", "OK");
        }
    }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        _logger.LogDebug("Wallet page disappearing, operations cancelled");
    }
}
