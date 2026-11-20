// <copyright file="PosLabPage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for POS Lab.</summary>
/// <remarks>Use: High during screen interactions. Scope: one page object.</remarks>
/// <param name="driver">The current Appium session.</param>
public class PosLabPage(AppiumDriver driver) : BasePage(driver)
{
    private static readonly By StartSessionButton = By.Id("PosStartSessionButton");
    private static readonly By SimulateButton = By.Id("PosSimulateButton");

    /// <summary>
    /// Starts session from PosLabPage. Use: High. Scope: PosLabPage.
    /// </summary>
    public PosLabPage StartSession()
    {
        ClickElement(StartSessionButton);
        return this;
    }

    /// <summary>
    /// Performs simulate for PosLabPage. Use: High. Scope: PosLabPage.
    /// </summary>
    public PosLabPage Simulate()
    {
        ClickElement(SimulateButton);
        return this;
    }

    /// <summary>
    /// Waits until the PosLabPage anchor control is visible. Use: High. Scope: PosLabPage.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(SimulateButton);
}
