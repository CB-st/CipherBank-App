// <copyright file="PrefsSyncAndStreamTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public class PrefsSyncAndStreamTests
{
    [Fact]
    public async Task PrefsSync_RoundTripsThroughMockApi()
    {
        var store = new MemPrefs();
        var api = new MockProductApi();
        var sync = new PrefsSyncService(store, api);

        store.Current.CoraEnabled = false;
        store.Current.AssetsLayout = "combined";
        (await sync.SaveAndPushAsync(store.Current, default)).Should().BeTrue();

        store.Current = new UserPrefs { CoraEnabled = true, AssetsLayout = "separate" };
        await sync.PullMergeAsync(default);
        store.Current.CoraEnabled.Should().BeFalse();
        store.Current.AssetsLayout.Should().Be("combined");
    }

    [Fact]
    public void PrefsMerge_KeepsLocalAssetsLayout_WhenRemoteOmitsIt()
    {
        var local = new UserPrefs { AssetsLayout = "combined", CoraEnabled = true };
        var remote = new PrefsWireDto { CoraEnabled = false };
        PrefsMerge.Merge(local, remote);
        local.AssetsLayout.Should().Be("combined");
        local.CoraEnabled.Should().BeFalse();
    }

    [Fact]
    public void StreamHub_FansOutOnce_WithoutDoubleSubscribe()
    {
        var stream = new MockStreamService();
        var hub = new StreamHub(stream);
        var count = 0;
        hub.EventReceived += (_, _) => count++;
        hub.Start();
        hub.Start();
        stream.Emit(new StreamEventArgs { Type = "RATE.TICK" });
        count.Should().Be(1);
        hub.StopStreaming();
        stream.Emit(new StreamEventArgs { Type = "RATE.TICK" });
        count.Should().Be(1);
    }

    [Fact]
    public async Task EventDebouncer_CoalescesBursts()
    {
        var debounce = new EventDebouncer(TimeSpan.FromMilliseconds(40));
        var runs = 0;
        await Task.WhenAll(
            debounce.DebounceAsync(
                () =>
                {
                    Interlocked.Increment(ref runs);
                    return Task.CompletedTask;
                },
                default),
            debounce.DebounceAsync(
                () =>
                {
                    Interlocked.Increment(ref runs);
                    return Task.CompletedTask;
                },
                default),
            debounce.DebounceAsync(
                () =>
                {
                    Interlocked.Increment(ref runs);
                    return Task.CompletedTask;
                },
                default));
        await Task.Delay(80);
        runs.Should().Be(1);
        debounce.FireCount.Should().Be(1);
    }

    [Fact]
    public void AccountBootstrapDto_HasNoSeedFields()
    {
        typeof(AccountBootstrapDto).GetProperties()
            .Select(p => p.Name.ToLowerInvariant())
            .Should()
            .NotContain(n => n.Contains("mnemonic") || n.Contains("seed") || n.Contains("pin"));
        typeof(BootstrapRecipientDto).GetProperties()
            .Select(p => p.Name.ToLowerInvariant())
            .Should()
            .NotContain(n => n.Contains("mnemonic") || n.Contains("seed"));
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
