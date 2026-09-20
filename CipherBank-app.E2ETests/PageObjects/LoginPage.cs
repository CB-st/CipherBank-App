// <copyright file="LoginPage.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Page object for the Login page.
/// Provides methods to interact with login functionality.
/// </summary>
public class LoginPage : BasePage
{
    // Element locators
    private static readonly By UsernameField = By.Id("UsernameEntry");
    private static readonly By PasswordField = By.Id("PasswordEntry");
    private static readonly By LoginButton = By.Id("LoginButton");
    private static readonly By ErrorMessage = By.Id("ErrorLabel");
    private static readonly By BiometricButton = By.Id("BiometricLoginButton");

    public LoginPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Enters the username.
    /// </summary>
    public LoginPage EnterUsername(string username)
    {
        EnterText(UsernameField, username);
        return this;
    }

    /// <summary>
    /// Enters the password.
    /// </summary>
    public LoginPage EnterPassword(string password)
    {
        EnterText(PasswordField, password);
        return this;
    }

    /// <summary>
    /// Clicks the login button.
    /// </summary>
    public void ClickLogin()
    {
        ClickElement(LoginButton);
    }

    /// <summary>
    /// Performs a complete login with username and password.
    /// Returns DashboardPage on success.
    /// </summary>
    public DashboardPage LoginAs(string username, string password)
    {
        EnterUsername(username);
        EnterPassword(password);
        ClickLogin();
        return new DashboardPage(Driver);
    }

    /// <summary>
    /// Checks if an error message is displayed.
    /// </summary>
    public bool IsErrorDisplayed()
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
    /// Checks if biometric login is available.
    /// </summary>
    public bool IsBiometricLoginAvailable()
    {
        return IsElementDisplayed(BiometricButton);
    }

    /// <summary>
    /// Attempts biometric login.
    /// </summary>
    public DashboardPage LoginWithBiometric()
    {
        ClickElement(BiometricButton);
        return new DashboardPage(Driver);
    }

    public override void WaitForPageLoad()
    {
        WaitForElement(LoginButton);
    }
}
