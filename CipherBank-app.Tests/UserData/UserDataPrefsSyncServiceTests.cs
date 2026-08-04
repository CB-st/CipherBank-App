// <copyright file="UserDataPrefsSyncServiceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.UserData;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.UserData;

public class UserDataPrefsSyncServiceTests
{
    private const string FixtureMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Fact]
    public async Task PackOnly_SaveAndPull_RoundTripsPrefs()
    {
        MemPrefs store = new();
        MockProductApi productImpl = new();
        IProductApi product = productImpl;
        IPrefsSyncService sync = CreateSync(store, productImpl, UserDataPrefsSyncOptions.PackOnly());

        store.Current.CoraEnabled = false;
        store.Current.Appearance = "light";
        store.Current.AssetsLayout = "combined";
        (await sync.SaveAndPushAsync(store.Current)).Should().BeTrue();

        store.Current = new UserPrefs { CoraEnabled = true, Appearance = "dark", AssetsLayout = "separate" };
        await sync.PullMergeAsync();

        store.Current.CoraEnabled.Should().BeFalse();
        store.Current.Appearance.Should().Be("light");
        store.Current.AssetsLayout.Should().Be("combined");

        PrefsWireDto? productPrefs = await product.GetPrefsAsync();
        productPrefs!.CoraEnabled.Should().BeTrue(); // default mock prefs untouched
    }

    [Fact]
    public async Task DualWrite_PushesProductUntilSuccessThreshold()
    {
        MemPrefs store = new();
        MockProductApi productImpl = new();
        IProductApi product = productImpl;
        UserDataPrefsSyncOptions options = new()
        {
            DualWriteProductPrefs = true,
            EnablePackSync = true,
            DisableProductPushAfterSuccessfulPackWrites = 2,
        };
        IPrefsSyncService sync = CreateSync(store, productImpl, options);

        store.Current.CoraEnabled = false;
        await sync.SaveAndPushAsync(store.Current);
        (await product.GetPrefsAsync())!.CoraEnabled.Should().BeFalse();

        store.Current.CoraEnabled = true;
        await sync.SaveAndPushAsync(store.Current);
        (await product.GetPrefsAsync())!.CoraEnabled.Should().BeTrue();

        store.Current.CoraEnabled = false;
        await sync.SaveAndPushAsync(store.Current);
        (await product.GetPrefsAsync())!.CoraEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Pull_FallsBackToProduct_WhenNoPack()
    {
        MemPrefs store = new();
        MockProductApi productImpl = new();
        IProductApi product = productImpl;
        await product.PutPrefsAsync(new PrefsWireDto { CoraEnabled = false, Appearance = "light" });

        IPrefsSyncService sync = CreateSync(store, productImpl, UserDataPrefsSyncOptions.DualWrite());
        store.Current.CoraEnabled = true;
        await sync.PullMergeAsync();
        store.Current.CoraEnabled.Should().BeFalse();
        store.Current.Appearance.Should().Be("light");
    }

    [Fact]
    public async Task Pull_PrefersPackOverProduct()
    {
        MemPrefs store = new();
        MockProductApi productImpl = new();
        IProductApi product = productImpl;
        IPrefsSyncService sync = CreateSync(store, productImpl, UserDataPrefsSyncOptions.PackOnly());

        store.Current.CoraEnabled = false;
        await sync.SaveAndPushAsync(store.Current);

        await product.PutPrefsAsync(new PrefsWireDto { CoraEnabled = true });

        store.Current = new UserPrefs { CoraEnabled = true };
        await sync.PullMergeAsync();
        store.Current.CoraEnabled.Should().BeFalse();
    }

    private static UserDataPrefsSyncService CreateSync(
        MemPrefs store,
        MockProductApi product,
        UserDataPrefsSyncOptions options)
    {
        MutableUserDataAccountContext account = new()
        {
            Username = "alice",
            Mnemonic = FixtureMnemonic,
        };
        return new UserDataPrefsSyncService(
            store,
            product,
            new MockUserDataClient(),
            account,
            new InMemoryUserDataPackMetaStore(),
            options);
    }

    private sealed class MemPrefs : IPrefsStore
    {
        public UserPrefs Current { get; set; } = new();

        public Task<UserPrefs> LoadAsync() => Task.FromResult(Current);

        public Task SaveAsync(UserPrefs prefs)
        {
            Current = prefs;
            return Task.CompletedTask;
        }
    }
}
