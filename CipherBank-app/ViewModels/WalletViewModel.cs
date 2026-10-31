// <copyright file="WalletViewModel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
/// ViewModel for the Wallet page displaying user's cryptocurrency wallets and transactions.
/// </summary>
public partial class WalletViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<WalletViewModel> _logger;
    private readonly IProductClient _product;
    private readonly IErrorHandler _errorHandler;
    private readonly IDialogService _dialog;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>Gets or sets the user's wallets.</summary>
    [ObservableProperty]
    public partial ObservableCollection<Wallet> Wallets { get; set; } = [];

    /// <summary>Gets or sets the transactions for the selected wallet.</summary>
    [ObservableProperty]
    public partial ObservableCollection<Transaction> Transactions { get; set; } = [];

    /// <summary>Gets or sets the selected wallet.</summary>
    [ObservableProperty]
    public partial Wallet? SelectedWallet { get; set; }

    /// <summary>Gets or sets the wallet cards shown in the deck.</summary>
    [ObservableProperty]
    public partial ObservableCollection<WalletCardItem> WalletCards { get; set; } = [];

    /// <summary>Gets or sets the focused wallet card.</summary>
    [ObservableProperty]
    public partial WalletCardItem? FocusedWalletCard { get; set; }

    /// <summary>Gets or sets the total wallet balance.</summary>
    [ObservableProperty]
    public partial decimal TotalBalance { get; set; }

    /// <summary>Gets or sets the total wallet balance in USD.</summary>
    [ObservableProperty]
    public partial decimal TotalBalanceUsd { get; set; }

    /// <summary>Gets or sets a value indicating whether wallets are loading.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Gets or sets a value indicating whether transactions are loading.</summary>
    [ObservableProperty]
    public partial bool IsLoadingTransactions { get; set; }

    /// <summary>Gets or sets the wallet error message.</summary>
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the send destination address.</summary>
    [ObservableProperty]
    public partial string SendToAddress { get; set; } = string.Empty;

    /// <summary>Gets or sets the send amount.</summary>
    [ObservableProperty]
    public partial decimal SendAmount { get; set; }

    /// <summary>Gets or sets a value indicating whether a refresh is in progress.</summary>
    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    /// <summary>Gets or sets a value indicating whether a send is in progress.</summary>
    [ObservableProperty]
    public partial bool IsSending { get; set; }

    public WalletViewModel(
        ILogger<WalletViewModel> logger,
        IProductClient product,
        IErrorHandler errorHandler,
        INavigationService navigation,
        IDialogService dialog)
    {
        _logger = logger;
        _product = product;
        _errorHandler = errorHandler;
        _ = navigation; // Reserved for future navigation needs
        _dialog = dialog;
    }

    /// <summary>
    /// Cancels any ongoing operations when leaving the page.
    /// </summary>
    public void OnDisappearing()
    {
        _cts?.Cancel();
        LogWalletDisappearing(_logger);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Note: WalletCardItem is a record — the generated setter uses value equality, so this
    // handler only fires when the card's data actually differs from the current one.
    partial void OnFocusedWalletCardChanged(WalletCardItem? value)
    {
        var newWallet = value?.Wallet;
        bool walletChanged = newWallet?.Id != SelectedWallet?.Id;

        SelectedWallet = newWallet;

        if (walletChanged)
        {
            SendToAddress = string.Empty;
            SendAmount = 0;
        }
    }

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
            LogLoadingWallets(_logger);

            var success = await _errorHandler.HandleApiErrorsAsync(
                async () =>
                {
                    PortfolioDto portfolio = await _product.GetPortfolioAsync(_cts.Token);

                    var previousFocusId = FocusedWalletCard?.Wallet.Id;
                    Wallets.Clear();
                    WalletCards.Clear();
                    decimal totalUsd = 0;

                    foreach (HoldingDto holding in portfolio.Holdings)
                    {
                        Wallet wallet = ProductSurfaceMap.ToWallet(holding);
                        Wallets.Add(wallet);
                        WalletCardItem card = ProductSurfaceMap.ToWalletCard(holding);
                        totalUsd += card.UsdValue;
                        WalletCards.Add(card);
                    }

                    TotalBalanceUsd = totalUsd;

                    FocusedWalletCard =
                        WalletCards.FirstOrDefault(c => c.Wallet.Id == previousFocusId)
                        ?? WalletCards.FirstOrDefault();

                    LogLoadedWallets(_logger, portfolio.Holdings.Count, TotalBalanceUsd);
                },
                msg => ErrorMessage = msg);

            if (!success)
            {
                if (ErrorMessage == null)
                {
                    LogLoadWalletsCancelled(_logger);
                }

                return;
            }
        }
        catch (Exception ex)
        {
            LogUnexpectedErrorLoadingWallets(_logger, ex);
            ErrorMessage = "Failed to load wallets. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes wallets via pull-to-refresh.
    /// </summary>
    [RelayCommand]
    private async Task RefreshWalletsAsync()
    {
        try
        {
            await LoadWalletsAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Loads transaction history for the selected wallet.
    /// </summary>
    [RelayCommand]
    private async Task LoadTransactionsAsync()
    {
        if (SelectedWallet == null || IsLoadingTransactions)
        {
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsLoadingTransactions = true;

        try
        {
            LogLoadingTransactions(_logger, SelectedWallet.Id);
            Transactions.Clear();
            LogLoadedTransactions(_logger, 0);
        }
        catch (OperationCanceledException)
        {
            LogLoadTransactionsCancelled(_logger);
        }
        catch (Exception ex)
        {
            LogErrorLoadingTransactions(_logger, ex);
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
            await _dialog.ShowAlertAsync("Error", "Please select a wallet first.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(SendToAddress))
        {
            await _dialog.ShowAlertAsync("Error", "Please enter a destination address.", "OK");
            return;
        }

        if (SendAmount <= 0)
        {
            await _dialog.ShowAlertAsync("Error", "Please enter a valid amount.", "OK");
            return;
        }

        if (SendAmount > SelectedWallet.Balance)
        {
            var insufficientMessage = $"Insufficient balance. Available: {SelectedWallet.FormattedBalance}";
            await _dialog.ShowAlertAsync("Error", insufficientMessage, "OK");
            return;
        }

        // Confirm transaction
        var confirmMessage = $"Send {SendAmount:F8} {SelectedWallet.CryptoSymbol} to:\n{SendToAddress}";
        var confirm = await _dialog.ShowConfirmAsync(
            "Confirm Send",
            confirmMessage,
            "Send",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsSending = true;
        ErrorMessage = null;

        try
        {
            LogSendingCrypto(_logger, SendAmount, SelectedWallet.CryptoSymbol.Value, SendToAddress);

            MoneyMoveDto move = await _product.TransferAsync(
                SendToAddress,
                SendAmount.ToString(CultureInfo.InvariantCulture),
                "standard",
                Guid.NewGuid().ToString("N"),
                _cts.Token);

            await _dialog.ShowAlertAsync(
                "Success",
                $"Transaction submitted!\nID: {move.Id}\nStatus: {move.Status}",
                "OK");

            // Clear form and refresh
            SendToAddress = string.Empty;
            SendAmount = 0;
            await LoadWalletsAsync();
            await LoadTransactionsAsync();

            LogSendCompleted(_logger, move.Id);
        }
        catch (InvalidOperationException ex)
        {
            LogSendFailed(_logger, ex, ex.Message);
            await _dialog.ShowAlertAsync("Transaction Failed", ex.Message, "OK");
        }
        catch (ArgumentException ex)
        {
            LogInvalidSendParameters(_logger, ex);
            await _dialog.ShowAlertAsync("Invalid Input", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            LogErrorSendingCrypto(_logger, ex);
            await _dialog.ShowAlertAsync(
                "Error",
                "Failed to send transaction. Please try again.",
                "OK");
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
        if (string.IsNullOrWhiteSpace(cryptoSymbol))
        {
            return;
        }

        try
        {
            LogCreatingWallet(_logger, cryptoSymbol);
            CreateWalletResultDto created = await _product.CreateWalletAsync(
                new CreateWalletRequestDto { Symbol = cryptoSymbol },
                CancellationToken.None);
            Wallet wallet = ProductSurfaceMap.ToWallet(created);

            Wallets.Add(wallet);
            SelectedWallet = wallet;

            await _dialog.ShowAlertAsync(
                "Wallet Created",
                $"New {wallet.CryptoName} wallet created!\nAddress: {wallet.Address}",
                "OK");

            LogCreatedWallet(_logger, wallet.Id, cryptoSymbol);
        }
        catch (InvalidOperationException ex)
        {
            LogCouldNotCreateWallet(_logger, ex, ex.Message);
            await _dialog.ShowAlertAsync("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            LogErrorCreatingWallet(_logger, ex);
            await _dialog.ShowAlertAsync(
                "Error",
                "Failed to create wallet. Please try again.",
                "OK");
        }
    }

#pragma warning disable SA1204 // Static members should appear before non-static members - LoggerMessage source generators
    [LoggerMessage(Level = LogLevel.Information, Message = "Loading wallets")]
    private static partial void LogLoadingWallets(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not get price for {Symbol}")]
    private static partial void LogCouldNotGetPrice(ILogger logger, Exception ex, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {Count} wallets with total value ${Value:N2}")]
    private static partial void LogLoadedWallets(ILogger logger, int count, decimal value);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Load wallets operation was cancelled")]
    private static partial void LogLoadWalletsCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error loading wallets")]
    private static partial void LogUnexpectedErrorLoadingWallets(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loading transactions for wallet {WalletId}")]
    private static partial void LogLoadingTransactions(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {Count} transactions")]
    private static partial void LogLoadedTransactions(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Load transactions operation was cancelled")]
    private static partial void LogLoadTransactionsCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading transactions")]
    private static partial void LogErrorLoadingTransactions(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending {Amount} {Symbol} to {Address}")]
    private static partial void LogSendingCrypto(ILogger logger, decimal amount, string symbol, string address);

    [LoggerMessage(Level = LogLevel.Information, Message = "Send completed. Transaction ID: {TransactionId}")]
    private static partial void LogSendCompleted(ILogger logger, string transactionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Send failed: {Message}")]
    private static partial void LogSendFailed(ILogger logger, Exception ex, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid send parameters")]
    private static partial void LogInvalidSendParameters(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error sending crypto")]
    private static partial void LogErrorSendingCrypto(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating wallet for {Symbol}")]
    private static partial void LogCreatingWallet(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created wallet {WalletId} for {Symbol}")]
    private static partial void LogCreatedWallet(ILogger logger, string walletId, string symbol);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not create wallet: {Message}")]
    private static partial void LogCouldNotCreateWallet(ILogger logger, Exception ex, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error creating wallet")]
    private static partial void LogErrorCreatingWallet(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Wallet page disappearing, operations cancelled")]
    private static partial void LogWalletDisappearing(ILogger logger);
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
