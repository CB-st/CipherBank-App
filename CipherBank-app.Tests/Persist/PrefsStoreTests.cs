// <copyright file="PrefsStoreTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class PrefsStoreTests
{
    [Fact]
    public async Task SaveLoad_IdleSecondsRoundTrip()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-prefs-" + Guid.NewGuid().ToString("N") + ".db");
        var db = new LocalDb(path);
        await db.InitializeAsync();
        var store = new PrefsStore(db);
        UserPrefs prefs = await store.LoadAsync();
        prefs.LockIdleSeconds = 90;
        prefs.Appearance = "light";
        prefs.HomeVisible["cora"] = false;
        await store.SaveAsync(prefs);

        UserPrefs loaded = await store.LoadAsync();
        loaded.LockIdleSeconds.Should().Be(90);
        loaded.Appearance.Should().Be("light");
        loaded.HomeVisible["cora"].Should().BeFalse();
    }
}
