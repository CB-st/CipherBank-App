// <copyright file="BasePage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.Support;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interfaces;
using OpenQA.Selenium.Support.UI;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Base page object class providing common functionality for all page objects.
/// Implements the Page Object Model pattern for maintainable UI tests.
/// </summary>
public abstract class BasePage
{
    /// <summary>
    /// Default wait timeout for element visibility.
    /// </summary>
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    protected BasePage(AppiumDriver driver)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Wait = new WebDriverWait(driver, DefaultTimeout);
    }

    /// <summary>Shared Appium session for this page object.</summary>
    protected AppiumDriver Driver { get; }

    /// <summary>Explicit wait bound to <see cref="DefaultTimeout"/>.</summary>
    protected WebDriverWait Wait { get; }

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
    /// Taps a Shell tab (or any control) by visible text — used when AutomationId is not on the tab bar.
    /// </summary>
    protected void TapByText(string text)
    {
        By xpath = By.XPath($"//*[@text='{text}' or @content-desc='{text}' or @label='{text}']");
        ClickElement(xpath);
    }

    /// <summary>
    /// Brings a control that lives further down a scrolling page into the viewport, then returns it.
    /// Controls scrolled out of view are absent from Android's accessibility tree, so a plain FindElement
    /// would fail on long pages (Profile) even though the control exists.
    /// Use: Medium (long-page interactions). Scope: the page's scrollable container.
    /// </summary>
    protected IWebElement ScrollTo(string automationId)
    {
        By locator = By.Id(automationId);
        if (IsElementDisplayed(locator))
        {
            return WaitForElement(locator);
        }

        string resourceId = $"{EmulatorReset.ResolvePackageId()}:id/{automationId}";
        try
        {
            Driver.FindElement(MobileBy.AndroidUIAutomator(
                "new UiScrollable(new UiSelector().scrollable(true)).setAsVerticalList()"
                + $".scrollIntoView(new UiSelector().resourceId(\"{resourceId}\"))"));
        }
        catch (WebDriverException)
        {
            // Intentional: let the wait below report the missing control rather than a scroll-strategy error.
        }

        return WaitForElement(locator);
    }

    /// <summary>
    /// Dismisses the soft keyboard when one is up, so entries near the bottom of a page do not leave their
    /// submit control covered. No-op when nothing is showing.
    /// Use: Medium (after typing into lower-page entries). Scope: current device session.
    /// </summary>
    protected void HideKeyboard()
    {
        if (Driver is not IHidesKeyboard keyboard)
        {
            return;
        }

        try
        {
            keyboard.HideKeyboard();
        }
        catch (WebDriverException)
        {
            // No keyboard up (or the driver refused): nothing to dismiss.
        }
    }

    /// <summary>
    /// Waits until a label carries non-empty text and returns it, so callers never read a bound label in the
    /// gap between it becoming visible and its value arriving.
    /// Use: Medium (assertions on async-populated labels). Scope: this page object.
    /// </summary>
    protected string WaitForNonEmptyText(By locator)
    {
        Wait.Until(_ => IsElementDisplayed(locator) && !string.IsNullOrWhiteSpace(GetElementText(locator)));
        return GetElementText(locator);
    }
}
