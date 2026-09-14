// <copyright file="WalletPage.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Page object for the Wallet page.
/// Provides methods to interact with wallet and transaction functionality.
/// </summary>
public class WalletPage : BasePage
{
    // Element locators
    private static readonly By _walletBalanceLabel = By.Id("WalletBalanceLabel");
    private static readonly By _walletAddressLabel = By.Id("WalletAddressLabel");
    private static readonly By _sendButton = By.Id("SendButton");
    private static readonly By _receiveButton = By.Id("ReceiveButton");
    private static readonly By _transactionHistoryList = By.Id("TransactionHistoryList");
    private static readonly By _copyAddressButton = By.Id("CopyAddressButton");
    private static readonly By _backButton = By.Id("BackButton");

    // Send transaction elements
    private static readonly By _recipientAddressField = By.Id("RecipientAddressEntry");
    private static readonly By _sendAmountField = By.Id("SendAmountEntry");
    private static readonly By _confirmSendButton = By.Id("ConfirmSendButton");
    private static readonly By _cancelSendButton = By.Id("CancelSendButton");

    public WalletPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Gets the wallet balance.
    /// </summary>
    public string GetBalance()
    {
        return GetElementText(_walletBalanceLabel);
    }

    /// <summary>
    /// Gets the wallet address.
    /// </summary>
    public string GetWalletAddress()
    {
        return GetElementText(_walletAddressLabel);
    }

    /// <summary>
    /// Copies the wallet address to clipboard.
    /// </summary>
    public WalletPage CopyAddress()
    {
        ClickElement(_copyAddressButton);
        return this;
    }

    /// <summary>
    /// Opens the send transaction dialog.
    /// </summary>
    public WalletPage OpenSendDialog()
    {
        ClickElement(_sendButton);
        return this;
    }

    /// <summary>
    /// Opens the receive dialog (shows QR code).
    /// </summary>
    public WalletPage OpenReceiveDialog()
    {
        ClickElement(_receiveButton);
        return this;
    }

    /// <summary>
    /// Enters the recipient address for a send transaction.
    /// </summary>
    public WalletPage EnterRecipientAddress(string address)
    {
        EnterText(_recipientAddressField, address);
        return this;
    }

    /// <summary>
    /// Enters the amount to send.
    /// </summary>
    public WalletPage EnterSendAmount(decimal amount)
    {
        EnterText(_sendAmountField, amount.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    /// <summary>
    /// Confirms the send transaction.
    /// </summary>
    public WalletPage ConfirmSend()
    {
        ClickElement(_confirmSendButton);
        return this;
    }

    /// <summary>
    /// Cancels the send transaction.
    /// </summary>
    public WalletPage CancelSend()
    {
        ClickElement(_cancelSendButton);
        return this;
    }

    /// <summary>
    /// Performs a complete send transaction.
    /// </summary>
    public WalletPage SendCrypto(string recipientAddress, decimal amount)
    {
        OpenSendDialog();
        EnterRecipientAddress(recipientAddress);
        EnterSendAmount(amount);
        ConfirmSend();
        return this;
    }

    /// <summary>
    /// Checks if transaction history is displayed.
    /// </summary>
    public bool HasTransactionHistory()
    {
        return IsElementDisplayed(_transactionHistoryList);
    }

    /// <summary>
    /// Goes back to the dashboard.
    /// </summary>
    public DashboardPage GoBack()
    {
        ClickElement(_backButton);
        return new DashboardPage(Driver);
    }

    public override void WaitForPageLoad()
    {
        WaitForElement(_walletBalanceLabel);
    }
}
