// <copyright file="BasePage.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Support.UI;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Base page object class providing common functionality for all page objects.
/// Implements the Page Object Model pattern for maintainable UI tests.
/// </summary>
public abstract class BasePage
{
    protected readonly AppiumDriver Driver;
    protected readonly WebDriverWait Wait;

    /// <summary>
    /// Default wait timeout for element visibility.
    /// </summary>
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    protected BasePage(AppiumDriver driver)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Wait = new WebDriverWait(driver, DefaultTimeout);
    }

    /// <summary>
    /// Waits for an element to be visible and returns it.
    /// </summary>
    protected IWebElement WaitForElement(By locator)
    {
        return Wait.Until(driver =>
        {
            var element = driver.FindElement(locator);
            return element.Displayed ? element : null;
        }) ?? throw new NoSuchElementException($"Element not found: {locator}");
    }

    /// <summary>
    /// Waits for an element to be clickable and clicks it.
    /// </summary>
    protected void ClickElement(By locator)
    {
        var element = WaitForElement(locator);
        element.Click();
    }

    /// <summary>
    /// Enters text into an input field after clearing it.
    /// </summary>
    protected void EnterText(By locator, string text)
    {
        var element = WaitForElement(locator);
        element.Clear();
        element.SendKeys(text);
    }

    /// <summary>
    /// Gets the text content of an element.
    /// </summary>
    protected string GetElementText(By locator)
    {
        return WaitForElement(locator).Text;
    }

    /// <summary>
    /// Checks if an element is displayed.
    /// </summary>
    public bool IsElementDisplayed(By locator)
    {
        try
        {
            return Driver.FindElement(locator).Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for the page to be fully loaded.
    /// Override in derived classes to add page-specific wait conditions.
    /// </summary>
    public virtual void WaitForPageLoad()
    {
        // Default implementation - can be overridden
    }
}
