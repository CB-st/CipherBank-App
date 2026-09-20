// <copyright file="PrefsNormalizeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
        UserPrefs prefs = new UserPrefs();
        prefs.ReplaceHomeVisible(new Dictionary<string, bool>
        {
            ["cora"] = true,
            ["balance"] = true,
            ["assets"] = false,
        });
        prefs.ReplaceHomeOrder(["cora", "balance", "assets"]);

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
        UserPrefs prefs = new UserPrefs();
        prefs.ReplaceEnabledCurrencies([]);
        prefs.NormalizeHomeSections();
        prefs.EnabledCurrencies.Should().BeEquivalentTo(UserPrefs.DefaultEnabledCurrencies);
    }

    [Fact]
    public void Normalize_InvalidSendSpeed_DefaultsToInstant()
    {
        UserPrefs prefs = new UserPrefs { DefaultSendSpeed = "warp" };
        prefs.NormalizeHomeSections();
        prefs.DefaultSendSpeed.Should().Be("instant");
    }
}
