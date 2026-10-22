// <copyright file="SessionExpiryRoutingTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using CipherBank_app.Constants;
using CipherBank_app.Models;
using CipherBank_app.Services;
using CipherBank_app.ViewModels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.ViewModels;

public class SessionExpiryRoutingTests
{
    [Fact]
    public async Task LoadWallets_PriceUnauthorized_ClearsSessionAndRedirects()
    {
        Wallet wallet = SampleWallet();
        (WalletViewModel viewModel, Mock<INavigationService> navigation, Mock<IAuthService> auth, _) = CreateWalletViewModel(
            wallets => wallets
                .Setup(service => service.GetWalletsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([wallet]),
            transactions => transactions
                .Setup(service => service.GetTransactionHistoryAsync(wallet.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]),
            crypto => crypto
                .Setup(service => service.GetCryptoPriceAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(WrappedUnauthorized("Failed to retrieve price for BTC from server")));

        await viewModel.LoadWalletsCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().Be("Session expired. Please log in again.");
        viewModel.WalletCards.Should().BeEmpty();
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task LoadWallets_PriceFailureWithoutUnauthorized_KeepsSessionAndShowsCardWithoutPrice()
    {
        Wallet wallet = SampleWallet();
        (WalletViewModel viewModel, Mock<INavigationService> navigation, Mock<IAuthService> auth, _) = CreateWalletViewModel(
            wallets => wallets
                .Setup(service => service.GetWalletsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync([wallet]),
            transactions => transactions
                .Setup(service => service.GetTransactionHistoryAsync(wallet.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]),
            crypto => crypto
                .Setup(service => service.GetCryptoPriceAsync(It.IsAny<AssetSymbol>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(
                    "price failed",
                    new HttpRequestException("unavailable", null, HttpStatusCode.ServiceUnavailable))));

        await viewModel.LoadWalletsCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().BeNull();
        viewModel.WalletCards.Should().ContainSingle();
        viewModel.WalletCards[0].UsdValue.Should().Be(0m);
        auth.Verify(service => service.LogoutAsync(), Times.Never);
        navigation.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoadTransactions_WrappedUnauthorized_ClearsSessionAndRedirects()
    {
        Wallet wallet = SampleWallet();
        var redirected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        (WalletViewModel viewModel, Mock<INavigationService> navigation, Mock<IAuthService> auth, _) = CreateWalletViewModel(
            transactions: transactions => transactions
                .Setup(service => service.GetTransactionHistoryAsync(wallet.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(WrappedUnauthorized("Failed to retrieve transaction history from server")));
        navigation.Setup(service => service.GoToAsync(Routes.Login)).Returns(() =>
        {
            redirected.TrySetResult();
            return Task.CompletedTask;
        });

        viewModel.SelectedWallet = wallet;
        await redirected.Task.WaitAsync(TimeSpan.FromSeconds(5));

        viewModel.ErrorMessage.Should().Be("Session expired. Please log in again.");
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task SendCrypto_WrappedUnauthorized_ClearsSessionWithoutFailureAlert()
    {
        Wallet wallet = SampleWallet();
        (WalletViewModel viewModel, Mock<INavigationService> navigation, Mock<IAuthService> auth, Mock<IDialogService> dialog) =
            CreateWalletViewModel(
                transactions: transactions =>
                {
                    transactions
                        .Setup(service => service.GetTransactionHistoryAsync(wallet.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync([]);
                    transactions
                        .Setup(service => service.SendCryptoAsync(
                            wallet.Id,
                            It.IsAny<string>(),
                            It.IsAny<decimal>(),
                            It.IsAny<CancellationToken>()))
                        .ThrowsAsync(WrappedUnauthorized("Failed to process send from server"));
                });
        dialog.Setup(service => service.ShowConfirmAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        viewModel.SelectedWallet = wallet;
        viewModel.SendToAddress = new string('a', 32);
        viewModel.SendAmount = 0.1m;
        await viewModel.SendCryptoCommand.ExecuteAsync(null);

        viewModel.IsSending.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be("Session expired. Please log in again.");
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
        dialog.Verify(
            service => service.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendCrypto_FailureWithoutUnauthorized_ShowsAlertWithoutClearingSession()
    {
        Wallet wallet = SampleWallet();
        (WalletViewModel viewModel, Mock<INavigationService> navigation, Mock<IAuthService> auth, Mock<IDialogService> dialog) =
            CreateWalletViewModel(
                transactions: transactions =>
                {
                    transactions
                        .Setup(service => service.GetTransactionHistoryAsync(wallet.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync([]);
                    transactions
                        .Setup(service => service.SendCryptoAsync(
                            wallet.Id,
                            It.IsAny<string>(),
                            It.IsAny<decimal>(),
                            It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new InvalidOperationException("Insufficient balance for transaction"));
                });
        dialog.Setup(service => service.ShowConfirmAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        viewModel.SelectedWallet = wallet;
        viewModel.SendToAddress = new string('a', 32);
        viewModel.SendAmount = 0.1m;
        await viewModel.SendCryptoCommand.ExecuteAsync(null);

        viewModel.IsSending.Should().BeFalse();
        auth.Verify(service => service.LogoutAsync(), Times.Never);
        navigation.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);
        dialog.Verify(
            service => service.ShowAlertAsync("Transaction Failed", "Insufficient balance for transaction", "OK"),
            Times.Once);
    }

    [Fact]
    public async Task CreateWallet_WrappedUnauthorized_ClearsSessionWithoutFailureAlert()
    {
        (WalletViewModel viewModel, Mock<INavigationService> navigation, Mock<IAuthService> auth, Mock<IDialogService> dialog) =
            CreateWalletViewModel(
                wallets: wallets => wallets
                    .Setup(service => service.CreateWalletAsync(It.IsAny<AssetSymbol>()))
                    .ThrowsAsync(WrappedUnauthorized("Failed to create wallet from server")));

        await viewModel.CreateWalletCommand.ExecuteAsync("BTC");

        viewModel.Wallets.Should().BeEmpty();
        viewModel.ErrorMessage.Should().Be("Session expired. Please log in again.");
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
        dialog.Verify(
            service => service.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task PurchaseCrypto_WrappedUnauthorized_ClearsSessionWithoutFailureAlert()
    {
        var crypto = new CryptoCurrency("BTC", "Bitcoin", 100m, 1m, 1m, 1m, 1m, null);
        (PurchaseViewModel viewModel, Mock<INavigationService> navigation, Mock<IAuthService> auth, Mock<IDialogService> dialog) =
            CreatePurchaseViewModel(transactions => transactions
                .Setup(service => service.PurchaseCryptoAsync(
                    It.IsAny<AssetSymbol>(),
                    It.IsAny<decimal>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(WrappedUnauthorized("Failed to process purchase from server")));
        dialog.Setup(service => service.ShowConfirmAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        viewModel.SelectedCrypto = crypto;
        viewModel.Amount = 0.25m;
        await viewModel.PurchaseCryptoCommand.ExecuteAsync(null);

        viewModel.IsPurchasing.Should().BeFalse();
        viewModel.ErrorMessage.Should().Be("Session expired. Please log in again.");
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
        dialog.Verify(
            service => service.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    private static (
        WalletViewModel ViewModel,
        Mock<INavigationService> Navigation,
        Mock<IAuthService> Auth,
        Mock<IDialogService> Dialog) CreateWalletViewModel(
        Action<Mock<IWalletService>>? wallets = null,
        Action<Mock<ITransactionService>>? transactions = null,
        Action<Mock<ICryptoApiService>>? crypto = null)
    {
        var walletService = new Mock<IWalletService>();
        var transactionService = new Mock<ITransactionService>();
        var cryptoService = new Mock<ICryptoApiService>();
        var navigation = new Mock<INavigationService>();
        var auth = new Mock<IAuthService>();
        var dialog = new Mock<IDialogService>();
        wallets?.Invoke(walletService);
        transactions?.Invoke(transactionService);
        crypto?.Invoke(cryptoService);
        auth.Setup(service => service.LogoutAsync()).Returns(Task.CompletedTask);
        navigation.Setup(service => service.GoToAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        WalletViewModel viewModel = new(
            NullLogger<WalletViewModel>.Instance,
            walletService.Object,
            transactionService.Object,
            cryptoService.Object,
            new ErrorHandler(navigation.Object, auth.Object),
            navigation.Object,
            dialog.Object);
        return (viewModel, navigation, auth, dialog);
    }

    private static (
        PurchaseViewModel ViewModel,
        Mock<INavigationService> Navigation,
        Mock<IAuthService> Auth,
        Mock<IDialogService> Dialog) CreatePurchaseViewModel(Action<Mock<ITransactionService>> transactions)
    {
        var transactionService = new Mock<ITransactionService>();
        var navigation = new Mock<INavigationService>();
        var auth = new Mock<IAuthService>();
        var dialog = new Mock<IDialogService>();
        transactions(transactionService);
        auth.Setup(service => service.LogoutAsync()).Returns(Task.CompletedTask);
        navigation.Setup(service => service.GoToAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        PurchaseViewModel viewModel = new(
            NullLogger<PurchaseViewModel>.Instance,
            new Mock<ICryptoApiService>().Object,
            transactionService.Object,
            new ErrorHandler(navigation.Object, auth.Object),
            navigation.Object,
            dialog.Object);
        return (viewModel, navigation, auth, dialog);
    }

    private static Wallet SampleWallet()
        => new("wallet-1", "BTC", "Bitcoin", 1.5m, "bc1qtestaddress", DateTimeOffset.UtcNow);

    private static InvalidOperationException WrappedUnauthorized(string message)
        => new(message, new HttpRequestException("denied", null, HttpStatusCode.Unauthorized));
}
