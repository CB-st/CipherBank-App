// <copyright file="PrefsNormalizeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class PrefsNormalizeTests
{
    [Fact]
    public void Normalize_MigratesLegacyAssets_ToHoldingsAndLocal()
    {
        var prefs = new UserPrefs
        {
            HomeOrder = new List<string> { "cora", "balance", "assets" },
            HomeVisible = new Dictionary<string, bool>
            {
                ["cora"] = true,
                ["balance"] = true,
                ["assets"] = false,
            },
        };

        prefs.NormalizeHomeSections();

        prefs.HomeOrder.Should().Contain("holdings");
        prefs.HomeOrder.Should().Contain("localWallets");
        prefs.HomeOrder.Should().NotContain("assets");
        prefs.HomeVisible["holdings"].Should().BeFalse();
        prefs.HomeVisible["localWallets"].Should().BeFalse();
        prefs.AssetsLayout.Should().Be("separate");
    }

    [Fact]
    public void Normalize_EmptyEnabledCurrencies_Defaults()
    {
        var prefs = new UserPrefs { EnabledCurrencies = new List<string>() };
        prefs.NormalizeHomeSections();
        prefs.EnabledCurrencies.Should().BeEquivalentTo(UserPrefs.DefaultEnabledCurrencies);
    }

    [Fact]
    public void Normalize_InvalidSendSpeed_DefaultsToInstant()
    {
        var prefs = new UserPrefs { DefaultSendSpeed = "warp" };
        prefs.NormalizeHomeSections();
        prefs.DefaultSendSpeed.Should().Be("instant");
    }
}
