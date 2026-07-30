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
            api: new FailingSessionApi(),
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
        IProductApi? api = null,
        InMemoryProductSessionStore? productSessions = null)
    {
        prefs ??= new FakePrefs();
        recipients ??= new MemRecipients();
        api ??= new MockProductApi();
        productSessions ??= new InMemoryProductSessionStore();
        var stream = new MockStreamService();
        var hub = new StreamHub(stream);
        var prefsSync = new PrefsSyncService(prefs, api);
        var bootstrap = new AccountBootstrapService(api, prefs, recipients);
        return new AppSession(new AppSessionDeps(
            custody,
            api,
            stream,
            hub,
            new LocalWalletSeeder(wallets),
            prefs,
            prefsSync,
            bootstrap,
            productSessions,
            TimeProvider.System));
    }

    private sealed class MemStore : ISecureStore
    {
        private readonly Dictionary<string, string> _data = new();

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
        public List<AchRecipientRow> Rows { get; } = new();

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

    private sealed class FailingSessionApi : IProductApi
    {
        private readonly MockProductApi _inner = new();

        public Task<SessionDto> CreateSessionAsync(CancellationToken ct)
            => Task.FromException<SessionDto>(new InvalidOperationException("offline"));

        public Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct) => _inner.GetPortfolioAsync(ct);

        public Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct)
            => _inner.GetHistoryAsync(symbol, range, ct);

        public Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct)
            => _inner.CreateSessionChallengeAsync(accountPublicKeyWire, ct);

        public Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct)
            => _inner.EstablishKeyShareAsync(request, ct);

        public Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request, CancellationToken ct)
            => _inner.CreateWalletAsync(request, ct);

        public Task<QuoteDto> GetQuoteAsync(string from, string toAsset, CancellationToken ct)
            => _inner.GetQuoteAsync(from, toAsset, ct);

        public Task<MoneyMoveDto> ConvertAsync(string from, string toAsset, string amount, string idempotencyKey, CancellationToken ct)
            => _inner.ConvertAsync(from, toAsset, amount, idempotencyKey, ct);

        public Task<MoneyMoveDto> TransferAsync(string destination, string amount, string speed, string idempotencyKey, CancellationToken ct)
            => _inner.TransferAsync(destination, amount, speed, idempotencyKey, ct);

        public Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct)
            => _inner.PayAsync(amount, mix, idempotencyKey, ct);

        public Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct)
            => _inner.GetReceiveAsync(asset, ct);

        public Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct)
            => _inner.GetVaultCardsAsync(ct);

        public Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct)
            => _inner.AddVaultCardAsync(card, idempotencyKey, ct);

        public Task DeleteVaultCardAsync(string cardId, CancellationToken ct)
            => _inner.DeleteVaultCardAsync(cardId, ct);

        public Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct)
            => _inner.GetVaultBinariesAsync(ct);

        public Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct)
            => _inner.CreatePosSessionAsync(ct);

        public Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct)
            => _inner.AuthorizePosAsync(sessionId, ct);

        public Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct)
            => _inner.ConfirmPosAsync(sessionId, ct);

        public Task<PrefsWireDto?> GetPrefsAsync(CancellationToken ct)
            => _inner.GetPrefsAsync(ct);

        public Task PutPrefsAsync(PrefsWireDto prefs, CancellationToken ct)
            => _inner.PutPrefsAsync(prefs, ct);

        public Task<AccountBootstrapDto> GetAccountBootstrapAsync(CancellationToken ct)
            => _inner.GetAccountBootstrapAsync(ct);
    }
}
