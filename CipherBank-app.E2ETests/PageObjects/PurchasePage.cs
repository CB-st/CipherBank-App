// <copyright file="PurchasePage.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Page object for the Purchase page.
/// Provides methods to interact with cryptocurrency purchase functionality.
/// </summary>
public class PurchasePage : BasePage
{
    // Element locators
    private static readonly By _viewAllButton = By.Id("ViewAllButton");
    private static readonly By _amountField = By.Id("AmountEntry");
    private static readonly By _purchaseButton = By.Id("PurchaseButton");
    private static readonly By _confirmButton = By.Id("ConfirmPurchaseButton");
    private static readonly By _cancelButton = By.Id("CancelButton");
    private static readonly By _successMessage = By.Id("SuccessLabel");
    private static readonly By _errorMessage = By.Id("ErrorLabel");
    private static readonly By _estimatedCryptoLabel = By.Id("EstimatedCryptoLabel");
    private static readonly By _feeLabel = By.Id("FeeLabel");
    private static readonly By _backButton = By.Id("BackButton");

    public PurchasePage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Selects a cryptocurrency to purchase.
    /// </summary>
    public PurchasePage SelectCrypto(string symbol)
    {
        ClickElement(_viewAllButton);
        var cryptoOption = By.XPath($"//*[contains(@text, '{symbol}')]");
        ClickElement(cryptoOption);
        return this;
    }

    /// <summary>
    /// Enters the purchase amount.
    /// </summary>
    public PurchasePage EnterAmount(decimal amount)
    {
        EnterText(_amountField, amount.ToString("F2", CultureInfo.InvariantCulture));
        return this;
    }

    /// <summary>
    /// Clicks the purchase button to initiate the purchase.
    /// </summary>
    public PurchasePage ClickPurchase()
    {
        ClickElement(_purchaseButton);
        return this;
    }

    /// <summary>
    /// Confirms the purchase in the confirmation dialog.
    /// </summary>
    public PurchasePage ConfirmPurchase()
    {
        ClickElement(_confirmButton);
        return this;
    }

    /// <summary>
    /// Cancels the purchase.
    /// </summary>
    public PurchasePage CancelPurchase()
    {
        ClickElement(_cancelButton);
        return this;
    }

    /// <summary>
    /// Performs a complete purchase operation.
    /// </summary>
    public PurchasePage CompletePurchase(string symbol, decimal amount)
    {
        SelectCrypto(symbol);
        EnterAmount(amount);
        ClickPurchase();
        ConfirmPurchase();
        return this;
    }

    /// <summary>
    /// Checks if the purchase was successful.
    /// </summary>
    public bool IsPurchaseSuccessful()
    {
        return IsElementDisplayed(_successMessage);
    }

    /// <summary>
    /// Gets the success message text.
    /// </summary>
    public string GetSuccessMessage()
    {
        return GetElementText(_successMessage);
    }

    /// <summary>
    /// Checks if an error occurred.
    /// </summary>
    public bool HasError()
    {
        return IsElementDisplayed(_errorMessage);
    }

    /// <summary>
    /// Gets the error message text.
    /// </summary>
    public string GetErrorMessage()
    {
        return GetElementText(_errorMessage);
    }

    /// <summary>
    /// Gets the estimated crypto amount for the purchase.
    /// </summary>
    public string GetEstimatedCrypto()
    {
        return GetElementText(_estimatedCryptoLabel);
    }

    /// <summary>
    /// Gets the transaction fee.
    /// </summary>
    public string GetFee()
    {
        return GetElementText(_feeLabel);
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
        WaitForElement(_viewAllButton);
        WaitForElement(_amountField);
    }
}
