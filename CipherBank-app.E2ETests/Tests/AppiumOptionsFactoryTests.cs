// <copyright file="AppiumOptionsFactoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.E2ETests.Infrastructure;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Regression tests for Appium capability construction; these need no Appium server or device.
/// </summary>
public class AppiumOptionsFactoryTests
{
    private static readonly Func<string, string?> NoOverrides = _ => null;

    [Fact]
    public void CreateAndroid_BuildsCapabilitiesWithoutConflict()
    {
        AppiumOptions options = AppiumOptionsFactory.CreateAndroid(NoOverrides);

        ICapabilities caps = options.ToCapabilities();
        caps.GetCapability("appium:deviceName").Should().Be("Android Emulator");
        caps.GetCapability("appium:automationName").Should().Be("UiAutomator2");
        caps.GetCapability("platformName").Should().Be("Android");
    }

    [Fact]
    public void CreateIos_BuildsCapabilitiesWithoutConflict()
    {
        AppiumOptions options = AppiumOptionsFactory.CreateIos(NoOverrides);

        ICapabilities caps = options.ToCapabilities();
        caps.GetCapability("appium:deviceName").Should().Be("iPhone 15");
        caps.GetCapability("appium:platformVersion").Should().Be("17.0");
        caps.GetCapability("appium:automationName").Should().Be("XCUITest");
    }

    [Fact]
    public void CreateAndroid_UsesEnvironmentOverride()
    {
        AppiumOptions options = AppiumOptionsFactory.CreateAndroid(name => name == "ANDROID_DEVICE" ? "Pixel 8" : null);

        options.DeviceName.Should().Be("Pixel 8");
    }
}
