// <copyright file="AppSessionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Session;

public class AppSessionTests
{
    private sealed class MemStore : ISecureStore
    {
        private readonly Dictionary<string, string> _data = new();

        public Task SetAsync(string key, string value)
        {
            _data[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(string key)
            => Task.FromResult(_data.TryGetValue(key, out string? v) ? v : null);

        public Task RemoveAsync(string key)
        {
            _data.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWallets : IWalletRepository
    {
        public List<LocalWalletRow> Rows { get; } = new();

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
        public Task<UserPrefs> LoadAsync() => Task.FromResult(new UserPrefs { LockIdleSeconds = 1 });

        public Task SaveAsync(UserPrefs prefs) => Task.CompletedTask;
    }

    [Fact]
    public async Task FinishSetup_UnlocksSeedsWalletsAndSession()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        var wallets = new FakeWallets();
        var seeder = new LocalWalletSeeder(wallets);
        var session = new AppSession(custody, new MockProductApi(), new MockStreamService(), seeder, new FakePrefs(), new InMemoryProductSessionStore());

        string mnemonic = MnemonicHelper.Generate();
        await session.FinishCustodySetupAsync(mnemonic, "123456");

        session.IsUnlocked.Should().BeTrue();
        session.HasWallet.Should().BeTrue();
        session.AccessToken.Should().NotBeNullOrEmpty();
        wallets.Rows.Should().Contain(r => r.Symbol == "BTC");
        wallets.Rows.Should().Contain(r => r.Symbol == "ETH");
    }

    [Fact]
    public async Task IdleLock_LocksAfterTimeout()
    {
        var store = new MemStore();
        var pin = new PinService(store);
        var custody = new CustodyService(store, pin);
        var session = new AppSession(custody, new MockProductApi(), new MockStreamService(), new LocalWalletSeeder(new FakeWallets()), new FakePrefs(), new InMemoryProductSessionStore());
        await session.FinishCustodySetupAsync(MnemonicHelper.Generate(), "123456");
        session.IdleMs = 1;
        await Task.Delay(20);
        session.CheckIdleAndMaybeLock().Should().BeTrue();
        session.IsUnlocked.Should().BeFalse();
    }
}
