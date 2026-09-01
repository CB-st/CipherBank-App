// <copyright file="PrefsStoreTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.Persist.Entities;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class PrefsStoreTests
{
    [Fact]
    public async Task SaveLoad_IdleSecondsRoundTrip()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-prefs-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(new FileInfo(path));
        await db.InitializeAsync();
        PrefsStore store = new PrefsStore(db);
        UserPrefs prefs = await store.LoadAsync();
        prefs.LockIdleSeconds = 90;
        prefs.Appearance = "light";
        prefs.HomeVisible["cora"] = false;
        prefs.ReplaceHomeOrder(["balance", "cora", "quickActions", "performance", "holdings", "localWallets"]);
        prefs.ReplaceEnabledCurrencies(["BTC", "ETH"]);
        await store.SaveAsync(prefs);

        UserPrefs loaded = await store.LoadAsync();
        loaded.LockIdleSeconds.Should().Be(90);
        loaded.Appearance.Should().Be("light");
        loaded.HomeVisible["cora"].Should().BeFalse();
        loaded.HomeOrder.Should().Equal("balance", "cora", "quickActions", "performance", "holdings", "localWallets");
        loaded.EnabledCurrencies.Should().Equal("BTC", "ETH");
        loaded.HomeOrder.Should().HaveCount(6);
    }

    [Fact]
    public async Task LoadAsync_InvalidJson_ReturnsDefaults()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-prefs-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(new FileInfo(path));
        await db.InitializeAsync();
        await using (CipherBankDbContext context = await db.CreateContextAsync())
        {
            context.Preferences.Add(new PreferenceEntity { Key = "user_prefs", Value = "{not-json" });
            await context.SaveChangesAsync();
        }

        PrefsStore store = new PrefsStore(db);
        UserPrefs prefs = await store.LoadAsync();
        prefs.LockIdleSeconds.Should().Be(new UserPrefs().LockIdleSeconds);
        prefs.EnabledCurrencies.Should().Equal(UserPrefs.DefaultEnabledCurrencies);
    }
}
