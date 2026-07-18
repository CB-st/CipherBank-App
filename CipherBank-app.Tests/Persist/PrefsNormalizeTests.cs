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
    public void Normalize_InvalidAssetsLayout_DefaultsToSeparate()
    {
        var prefs = new UserPrefs { AssetsLayout = "weird" };
        prefs.NormalizeHomeSections();
        prefs.AssetsLayout.Should().Be("separate");
    }
}
