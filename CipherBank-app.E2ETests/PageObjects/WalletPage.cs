// <copyright file="WalletPage.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

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
    private static readonly By WalletBalanceLabel = By.Id("WalletBalanceLabel");
    private static readonly By WalletAddressLabel = By.Id("WalletAddressLabel");
    private static readonly By SendButton = By.Id("SendButton");
    private static readonly By ReceiveButton = By.Id("ReceiveButton");
    private static readonly By TransactionHistoryList = By.Id("TransactionHistoryList");
    private static readonly By CopyAddressButton = By.Id("CopyAddressButton");
    private static readonly By BackButton = By.Id("BackButton");

    // Send transaction elements
    private static readonly By RecipientAddressField = By.Id("RecipientAddressEntry");
    private static readonly By SendAmountField = By.Id("SendAmountEntry");
    private static readonly By ConfirmSendButton = By.Id("ConfirmSendButton");
    private static readonly By CancelSendButton = By.Id("CancelSendButton");

    public WalletPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Gets the wallet balance.
    /// </summary>
    public string GetBalance()
    {
        return GetElementText(WalletBalanceLabel);
    }

    /// <summary>
    /// Gets the wallet address.
    /// </summary>
    public string GetWalletAddress()
    {
        return GetElementText(WalletAddressLabel);
    }

    /// <summary>
    /// Copies the wallet address to clipboard.
    /// </summary>
    public WalletPage CopyAddress()
    {
        ClickElement(CopyAddressButton);
        return this;
    }

    /// <summary>
    /// Opens the send transaction dialog.
    /// </summary>
    public WalletPage OpenSendDialog()
    {
        ClickElement(SendButton);
        return this;
    }

    /// <summary>
    /// Opens the receive dialog (shows QR code).
    /// </summary>
    public WalletPage OpenReceiveDialog()
    {
        ClickElement(ReceiveButton);
        return this;
    }

    /// <summary>
    /// Enters the recipient address for a send transaction.
    /// </summary>
    public WalletPage EnterRecipientAddress(string address)
    {
        EnterText(RecipientAddressField, address);
        return this;
    }

    /// <summary>
    /// Enters the amount to send.
    /// </summary>
    public WalletPage EnterSendAmount(decimal amount)
    {
        EnterText(SendAmountField, amount.ToString());
        return this;
    }

    /// <summary>
    /// Confirms the send transaction.
    /// </summary>
    public WalletPage ConfirmSend()
    {
        ClickElement(ConfirmSendButton);
        return this;
    }

    /// <summary>
    /// Cancels the send transaction.
    /// </summary>
    public WalletPage CancelSend()
    {
        ClickElement(CancelSendButton);
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
        return IsElementDisplayed(TransactionHistoryList);
    }

    /// <summary>
    /// Goes back to the dashboard.
    /// </summary>
    public DashboardPage GoBack()
    {
        ClickElement(BackButton);
        return new DashboardPage(Driver);
    }

    public override void WaitForPageLoad()
    {
        WaitForElement(WalletBalanceLabel);
    }
}
