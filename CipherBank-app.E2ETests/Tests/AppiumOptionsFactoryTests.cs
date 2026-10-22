// <copyright file="AppiumOptionsFactoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;
using System.Text.RegularExpressions;
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
        options.ToCapabilities().GetCapability("appium:deviceName").Should().Be("Pixel 8");
    }

    [Fact]
    public void CreateIos_UsesEnvironmentOverrides()
    {
        AppiumOptions options = AppiumOptionsFactory.CreateIos(name => name switch
        {
            "IOS_DEVICE" => "iPhone 16 Pro",
            "IOS_VERSION" => "18.1",
            "IOS_APP_PATH" => "/tmp/Custom.app",
            _ => null,
        });

        ICapabilities caps = options.ToCapabilities();
        caps.GetCapability("appium:deviceName").Should().Be("iPhone 16 Pro");
        caps.GetCapability("appium:platformVersion").Should().Be("18.1");
        caps.GetCapability("appium:app").Should().Be("/tmp/Custom.app");
    }

    [Fact]
    public void CreateAndroid_EmitsDeviceNameExactlyOnce()
    {
        AssertDeviceNameEmittedOnce(AppiumOptionsFactory.CreateAndroid(NoOverrides));
    }

    [Fact]
    public void CreateIos_EmitsDeviceNameExactlyOnce()
    {
        AssertDeviceNameEmittedOnce(AppiumOptionsFactory.CreateIos(NoOverrides));
    }

    [Fact]
    public void CreateAndroid_NullEnvironmentLookup_Throws()
    {
        Action act = () => AppiumOptionsFactory.CreateAndroid(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("getEnvironmentVariable");
    }

    [Fact]
    public void CreateIos_NullEnvironmentLookup_Throws()
    {
        Action act = () => AppiumOptionsFactory.CreateIos(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("getEnvironmentVariable");
    }

    private static void AssertDeviceNameEmittedOnce(AppiumOptions options)
    {
        // Serialize the W3C capability payload sent to the Appium server and count raw key occurrences,
        // so a duplicate key cannot be hidden by dictionary lookup semantics.
        string payload = JsonSerializer.Serialize(options.ToDictionary());

        Regex.Count(payload, "\"(appium:)?deviceName\":").Should().Be(1, payload);
    }
}
