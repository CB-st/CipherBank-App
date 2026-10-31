// <copyright file="ReceivePage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Receive tab.</summary>
public class ReceivePage : BasePage
{
    private static readonly By RefreshButton = By.Id("ReceiveRefreshButton");
    private static readonly By QrImage = By.Id("ReceiveQrImage");
    private static readonly By AddressLabel = By.Id("ReceiveAddressLabel");

    /// <summary>
    /// Initializes ReceivePage with its Appium/test collaborators. Use: High. Scope: one ReceivePage instance.
    /// </summary>
    public ReceivePage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Performs refresh Qr for ReceivePage. Use: High. Scope: ReceivePage.
    /// </summary>
    public ReceivePage RefreshQr()
    {
        ClickElement(RefreshButton);
        return this;
    }

    /// <summary>
    /// Reports whether qr Visible holds for ReceivePage. Use: High. Scope: ReceivePage.
    /// </summary>
    public bool IsQrVisible() => IsElementDisplayed(QrImage);

    /// <summary>
    /// Reads address from ReceivePage. Use: High. Scope: ReceivePage.
    /// </summary>
    public string GetAddress() => GetElementText(AddressLabel);

    /// <summary>
    /// Waits until the ReceivePage anchor control is visible. Use: High. Scope: ReceivePage.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(RefreshButton);
}
