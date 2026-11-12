// <copyright file="PrefsWireBoundaryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;
using CipherBank_app.Persist;
using CipherBank_app.Tests.Persist;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

/// <summary>Verifies collection aliases, invalid optional values, and canonical wire precedence.</summary>
public sealed class PrefsWireBoundaryTests
{
    /// <summary>Alternate collection keys ignore invalid members and fold exactly once.</summary>
    [Fact]
    public void FoldAlternateNames_FiltersCollectionMembersAndAcceptsLockAlias()
    {
        PrefsWireDto dto = JsonSerializer.Deserialize<PrefsWireDto>("""
            {"homeOrder":["balance",42,null,"holdings"],
             "homeVisible":{"balance":false,"holdings":true,"invalid":"true"},
             "enabledCurrencies":["USD",false,"EUR"],"lockIdleSeconds":75}
            """)!;

        dto.FoldAlternateNames();
        dto.FoldAlternateNames();

        dto.HomeOrder.Should().Equal("balance", "holdings");
        dto.HomeVisible.Should().BeEquivalentTo(new Dictionary<string, bool> { ["balance"] = false, ["holdings"] = true });
        dto.EnabledCurrencies.Should().Equal("USD", "EUR");
        dto.LockIdleSeconds.Should().Be(75);
        dto.ExtensionData.Should().BeNull();
    }

    /// <summary>Canonical values win over aliases, including false and existing collections.</summary>
    [Fact]
    public void FoldAlternateNames_PreservesCanonicalValues()
    {
        PrefsWireDto dto = JsonSerializer.Deserialize<PrefsWireDto>("""
            {"HOME_ORDER":["balance"],"homeOrder":["holdings"],
             "HOME_VISIBLE":{"balance":false},"homeVisible":{"balance":true},
             "ENABLED_CURRENCIES":["USD"],"enabledCurrencies":["EUR"],
             "CORA_ENABLED":false,"coraEnabled":true,"LOCK_IDLE_SECONDS":120,"appLockIdleSec":45}
            """)!;

        dto.FoldAlternateNames();

        dto.HomeOrder.Should().Equal("balance");
        dto.HomeVisible["balance"].Should().BeFalse();
        dto.EnabledCurrencies.Should().Equal("USD");
        dto.CoraEnabled.Should().BeFalse();
        dto.LockIdleSeconds.Should().Be(120);
    }

    /// <summary>Malformed optional aliases cannot overwrite local settings.</summary>
    [Theory]
    [InlineData("null")]
    [InlineData("\"wrong-type\"")]
    [InlineData("1.5")]
    public void ApplyOnto_InvalidOptionalAliases_PreservesLocalValues(string invalid)
    {
        PrefsWireDto dto = JsonSerializer.Deserialize<PrefsWireDto>(
            $$"""{"homeOrder":{{invalid}},"homeVisible":{{invalid}},"enabledCurrencies":{{invalid}},"coraEnabled":{{invalid}},"lockIdleSeconds":{{invalid}}}""")!;
        UserPrefs local = new(TestPreferenceDefaults.Value) { LockIdleSeconds = 180, CoraEnabled = true };
        local.ReplaceEnabledCurrencies(["USD", "EUR"]);

        dto.ApplyOnto(local);

        local.LockIdleSeconds.Should().Be(180);
        local.CoraEnabled.Should().BeTrue();
        local.EnabledCurrencies.Should().Equal("USD", "EUR");
    }

    /// <summary>A missing remote snapshot preserves and normalizes the local preferences.</summary>
    [Fact]
    public void Merge_WithoutRemote_ReturnsLocalPreferences()
    {
        UserPrefs local = new(TestPreferenceDefaults.Value) { AssetsLayout = "combined", LockIdleSeconds = 180 };

        UserPrefs merged = PrefsMerge.Merge(local, null);

        merged.Should().BeSameAs(local);
        merged.AssetsLayout.Should().Be("combined");
        merged.LockIdleSeconds.Should().Be(180);
        merged.HomeOrder.Should().OnlyHaveUniqueItems();
    }
}
