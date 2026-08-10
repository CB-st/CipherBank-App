// <copyright file="AppSessionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Http;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Session;

/// <summary>
/// AppSession unit coverage for finish-setup, idle lock, unlock bootstrap, and failed-session rollback.
/// Serialized via <see cref="AppSessionTestSerialGate"/>.
/// </summary>
[Collection(nameof(AppSessionTests))]
public class AppSessionTests
{
    [Fact]
    public async Task FinishSetup_UnlocksSeedsWalletsAndSession()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        var wallets = new FakeWallets();
        var productSessions = new InMemoryProductSessionStore();
        AppSession session = CreateSession(custody, wallets, productSessions: productSessions);

        var mnemonic = MnemonicHelper.Generate();
        await session.FinishCustodySetupAsync(mnemonic, "123456");

        session.IsUnlocked.Should().BeTrue();
        session.HasWallet.Should().BeTrue();
        session.AccessToken.Should().NotBeNullOrEmpty();
        (await productSessions.GetAsync()).Should().NotBeNull();
        wallets.Rows.Should().Contain(r => r.Symbol == "BTC");
        wallets.Rows.Should().Contain(r => r.Symbol == "ETH");
    }

    [Fact]
    public async Task IdleLock_LocksAfterTimeout()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        AppSession session = CreateSession(custody, new FakeWallets());
        await session.FinishCustodySetupAsync(MnemonicHelper.Generate(), "123456");
        session.IdleMs = 1;
        await Task.Delay(20);
        session.CheckIdleAndMaybeLock().Should().BeTrue();
        session.IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task Unlock_PullsBootstrapRecipients_WithoutTouchingCustodySeal()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        var recipients = new MemRecipients();
        var prefs = new FakePrefs();
        AppSession session = CreateSession(custody, new FakeWallets(), prefs, recipients);
        await session.FinishCustodySetupAsync(MnemonicHelper.Generate(), "123456");
        session.Lock();

        (await session.UnlockAsync("123456")).Should().BeTrue();
        recipients.Rows.Should().Contain(r => r.Name == "Maya Chen");
        recipients.Rows.Should().Contain(r => r.RoutingMask == AchRecipientValidation.MaskRouting("021000021")
            || r.Routing == "021000021");
        custody.IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Unlock_WhenCreateSessionFails_RelocksCustody()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        var productSessions = new InMemoryProductSessionStore();
        AppSession okSession = CreateSession(custody, new FakeWallets(), productSessions: productSessions);
        await okSession.FinishCustodySetupAsync(MnemonicHelper.Generate(), "123456");
        okSession.Lock();

        AppSession failing = CreateSession(
            custody,
            new FakeWallets(),
            api: CreateFailingApi(new InvalidOperationException("offline")),
            productSessions: productSessions);
        (await failing.UnlockAsync("123456")).Should().BeFalse();
        custody.IsUnlocked.Should().BeFalse();
        failing.AccessToken.Should().BeNull();
        (await productSessions.GetAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Unlock_WhenCreateSessionThrowsHttpRequestException_RelocksCustody()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        var productSessions = new InMemoryProductSessionStore();
        AppSession okSession = CreateSession(custody, new FakeWallets(), productSessions: productSessions);
        await okSession.FinishCustodySetupAsync(MnemonicHelper.Generate(), "123456");
        okSession.Lock();

        AppSession failing = CreateSession(
            custody,
            new FakeWallets(),
            api: CreateFailingApi(new HttpRequestException("offline")),
            productSessions: productSessions);
        (await failing.UnlockAsync("123456")).Should().BeFalse();
        custody.IsUnlocked.Should().BeFalse();
        failing.AccessToken.Should().BeNull();
        (await productSessions.GetAsync()).Should().BeNull();
    }

    private static AppSession CreateSession(
        ICustodyService custody,
        FakeWallets wallets,
        FakePrefs? prefs = null,
        MemRecipients? recipients = null,
        IProductClient? api = null,
        InMemoryProductSessionStore? productSessions = null)
    {
        prefs ??= new FakePrefs();
        recipients ??= new MemRecipients();
        api ??= new InMemoryProductClient();
        productSessions ??= new InMemoryProductSessionStore();
        var stream = new MockStreamService();
        var hub = new StreamHub(stream);
        var prefsSync = new PrefsSyncService(prefs, api);
        var bootstrap = new AccountBootstrapService(api, prefs, recipients);
        var productSession = new ProductSessionCoordinator(
            api,
            stream,
            hub,
            prefs,
            prefsSync,
            bootstrap,
            productSessions);
        return new AppSession(
            custody,
            productSession,
            new LocalWalletSeeder(wallets),
            prefs,
            TimeProvider.System);
    }

    private static IProductClient CreateFailingApi(Exception exception)
    {
        var api = new Mock<IProductClient>(MockBehavior.Strict);
        api.Setup(value => value.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<SessionDto>(exception));
        return api.Object;
    }

    private sealed class MemStore : ISecureStore
    {
        private readonly Dictionary<string, string> _data = [];

        public Task SetAsync(string key, string value)
        {
            _data[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
            => Task.FromResult(_data.TryGetValue(key, out var v) ? v : null);

        public Task RemoveAsync(string key)
        {
            _data.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWallets : IWalletRepository
    {
        public List<LocalWalletRow> Rows { get; } = [];

        public Task<IReadOnlyList<LocalWalletRow>> ListAsync()
            => Task.FromResult<IReadOnlyList<LocalWalletRow>>(Rows);

        public Task UpsertAsync(LocalWalletRow row)
        {
            Rows.RemoveAll(r => r.Id == row.Id);
            Rows.Add(row);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            Rows.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePrefs : IPrefsStore
    {
        public UserPrefs Current { get; set; } = new() { LockIdleSeconds = 1 };

        public Task<UserPrefs> LoadAsync() => Task.FromResult(Current);

        public Task SaveAsync(UserPrefs prefs)
        {
            Current = prefs;
            return Task.CompletedTask;
        }
    }

    private sealed class MemRecipients : IRecipientRepository
    {
        public List<AchRecipientRow> Rows { get; } = [];

        public Task EnsureSchemaAsync() => Task.CompletedTask;

        public Task<IReadOnlyList<AchRecipientRow>> ListAsync()
            => Task.FromResult<IReadOnlyList<AchRecipientRow>>(Rows);

        public Task UpsertAsync(AchRecipientRow row)
        {
            Rows.RemoveAll(r => r.Id == row.Id);
            Rows.Add(row);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            Rows.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }

        public Task SeedDefaultsIfEmptyAsync() => Task.CompletedTask;
    }
}
