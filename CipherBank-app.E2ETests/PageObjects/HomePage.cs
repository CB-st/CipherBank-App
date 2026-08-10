// <copyright file="HomePage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Home tab.</summary>
public class HomePage : BasePage
{
    private const string MoreTabLabel = "More";

    private static readonly By TotalUsd = By.Id("HomeTotalUsdLabel");
    private static readonly By HideBalances = By.Id("HomeHideBalancesButton");
    private static readonly By Range1d = By.Id("HomeRange1dButton");
    private static readonly By Range1w = By.Id("HomeRange1wButton");
    private static readonly By Range1m = By.Id("HomeRange1mButton");
    private static readonly By Range1y = By.Id("HomeRange1yButton");
    private static readonly By ConvertButton = By.Id("HomeConvertButton");
    private static readonly By SendButton = By.Id("HomeSendButton");
    private static readonly By ReceiveButton = By.Id("HomeReceiveButton");
    private static readonly By ProfileTabItem =
        By.XPath("//*[@text='Profile' or @content-desc='Profile' or @label='Profile']");

    /// <summary>
    /// Initializes HomePage with its Appium/test collaborators. Use: High. Scope: one HomePage instance.
    /// </summary>
    public HomePage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Reports whether the HomePage anchor control is visible. Use: High. Scope: HomePage.
    /// </summary>
    public bool IsLoaded() => IsElementDisplayed(TotalUsd);

    /// <summary>
    /// Reports whether chart Range Chips are visible on HomePage. Use: High. Scope: HomePage.
    /// </summary>
    public bool HasChartRangeChips()
        => IsElementDisplayed(Range1d)
           && IsElementDisplayed(Range1w)
           && IsElementDisplayed(Range1m)
           && IsElementDisplayed(Range1y);

    /// <summary>
    /// Reports whether hide Balances Toggle are visible on HomePage. Use: High. Scope: HomePage.
    /// </summary>
    public bool HasHideBalancesToggle() => IsElementDisplayed(HideBalances);

    /// <summary>
    /// Toggles hide Balances through HomePage. Use: High. Scope: HomePage.
    /// </summary>
    public HomePage ToggleHideBalances()
    {
        ClickElement(HideBalances);
        return this;
    }

    /// <summary>
    /// Selects range1w through HomePage. Use: High. Scope: HomePage.
    /// </summary>
    public HomePage SelectRange1w()
    {
        ClickElement(Range1w);
        return this;
    }

    /// <summary>
    /// Navigates from HomePage to Convert. Use: High. Scope: HomePage.
    /// </summary>
    public ConvertPage GoToConvert()
    {
        ClickElement(ConvertButton);
        return new ConvertPage(Driver);
    }

    /// <summary>
    /// Navigates from HomePage to Send. Use: High. Scope: HomePage.
    /// </summary>
    public SendPage GoToSend()
    {
        ClickElement(SendButton);
        return new SendPage(Driver);
    }

    /// <summary>Navigate via Shell tab bar when Home shortcuts are off-screen.</summary>
    public ConvertPage GoToConvertTab()
    {
        TapByText("Convert");
        return new ConvertPage(Driver);
    }

    /// <summary>
    /// Navigates from HomePage to Send Tab. Use: High. Scope: HomePage.
    /// </summary>
    public SendPage GoToSendTab()
    {
        TapByText("Send");
        return new SendPage(Driver);
    }

    /// <summary>
    /// Switches to the Profile tab (Profile has no Home shortcut button). AppShell declares six tabs but
    /// Android's BottomNavigationView only renders five, so Profile sits behind the "More" overflow; the
    /// overflow is opened only when Profile is not already on the bar, keeping this correct on layouts
    /// (tablet/desktop, or a future five-tab Shell) where every tab is visible.
    /// Use: High (Profile-rooted stories such as CB-ACCOUNT-PIN-CHANGE). Scope: this page object.
    /// </summary>
    public ProfilePage GoToProfileTab()
    {
        if (!IsElementDisplayed(ProfileTabItem))
        {
            TapByText(MoreTabLabel);
        }

        ClickElement(ProfileTabItem);
        return new ProfilePage(Driver);
    }

    /// <summary>
    /// Navigates from HomePage to Receive. Use: High. Scope: HomePage.
    /// </summary>
    public ReceivePage GoToReceive()
    {
        ClickElement(ReceiveButton);
        return new ReceivePage(Driver);
    }

    /// <summary>
    /// Waits until the HomePage anchor control is visible. Use: High. Scope: HomePage.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(TotalUsd);
}
