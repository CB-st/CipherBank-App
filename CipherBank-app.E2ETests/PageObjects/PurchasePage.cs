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
    private static readonly By ViewAllButton = By.Id("ViewAllButton");
    private static readonly By AmountField = By.Id("AmountEntry");
    private static readonly By PurchaseButton = By.Id("PurchaseButton");
    private static readonly By ConfirmButton = By.Id("ConfirmPurchaseButton");
    private static readonly By CancelButton = By.Id("CancelButton");
    private static readonly By SuccessMessage = By.Id("SuccessLabel");
    private static readonly By ErrorMessage = By.Id("ErrorLabel");
    private static readonly By EstimatedCryptoLabel = By.Id("EstimatedCryptoLabel");
    private static readonly By FeeLabel = By.Id("FeeLabel");
    private static readonly By BackButton = By.Id("BackButton");

    public PurchasePage(AppiumDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Selects a cryptocurrency to purchase.
    /// </summary>
    public PurchasePage SelectCrypto(string symbol)
    {
        ClickElement(ViewAllButton);
        var cryptoOption = By.XPath($"//*[contains(@text, '{symbol}')]");
        ClickElement(cryptoOption);
        return this;
    }

    /// <summary>
    /// Enters the purchase amount.
    /// </summary>
    public PurchasePage EnterAmount(decimal amount)
    {
        EnterText(AmountField, amount.ToString("F2"));
        return this;
    }

    /// <summary>
    /// Clicks the purchase button to initiate the purchase.
    /// </summary>
    public PurchasePage ClickPurchase()
    {
        ClickElement(PurchaseButton);
        return this;
    }

    /// <summary>
    /// Confirms the purchase in the confirmation dialog.
    /// </summary>
    public PurchasePage ConfirmPurchase()
    {
        ClickElement(ConfirmButton);
        return this;
    }

    /// <summary>
    /// Cancels the purchase.
    /// </summary>
    public PurchasePage CancelPurchase()
    {
        ClickElement(CancelButton);
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
        return IsElementDisplayed(SuccessMessage);
    }

    /// <summary>
    /// Gets the success message text.
    /// </summary>
    public string GetSuccessMessage()
    {
        return GetElementText(SuccessMessage);
    }

    /// <summary>
    /// Checks if an error occurred.
    /// </summary>
    public bool HasError()
    {
        return IsElementDisplayed(ErrorMessage);
    }

    /// <summary>
    /// Gets the error message text.
    /// </summary>
    public string GetErrorMessage()
    {
        return GetElementText(ErrorMessage);
    }

    /// <summary>
    /// Gets the estimated crypto amount for the purchase.
    /// </summary>
    public string GetEstimatedCrypto()
    {
        return GetElementText(EstimatedCryptoLabel);
    }

    /// <summary>
    /// Gets the transaction fee.
    /// </summary>
    public string GetFee()
    {
        return GetElementText(FeeLabel);
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
        WaitForElement(ViewAllButton);
        WaitForElement(AmountField);
    }
}
