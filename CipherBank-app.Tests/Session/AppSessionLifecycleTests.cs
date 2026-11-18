// <copyright file="AppSessionLifecycleTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net.WebSockets;
using System.Text.Json;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Session;
using CipherBank_app.Tests.Persist;
using CipherBank_app.Wallets;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Session;

/// <summary>Verifies session lifecycle boundaries using deterministic ports and time.</summary>
public sealed class AppSessionLifecycleTests
{
    /// <summary>Boot loads wallet presence and selects the configured or fallback idle timeout.</summary>
    [Theory]
    [InlineData(true, 180, 180000)]
    [InlineData(false, 0, 60000)]
    [InlineData(true, -1, 60000)]
    public async Task Boot_LoadsWalletAndIdlePolicy(bool hasWallet, int seconds, int expectedMs)
    {
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.HasSealedWalletAsync()).ReturnsAsync(hasWallet);
        Mock<IPrefsStore> prefs = new();
        prefs.Setup(p => p.LoadAsync())
            .ReturnsAsync(new UserPrefs(TestPreferenceDefaults.Value) { LockIdleSeconds = seconds });
        AppSession session = Create(custody, prefs: prefs.Object);

        session.IsBooting.Should().BeTrue();
        await session.BootAsync();

        session.IsBooting.Should().BeFalse();
        session.HasWallet.Should().Be(hasWallet);
        session.IdleMs.Should().Be(expectedMs);
    }

    /// <summary>A failed boot always leaves the booting state and propagates the failure.</summary>
    [Fact]
    public async Task Boot_WhenPreferencesFail_ClearsBootingFlag()
    {
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.HasSealedWalletAsync()).ReturnsAsync(true);
        Mock<IPrefsStore> prefs = new();
        prefs.Setup(p => p.LoadAsync()).ThrowsAsync(new InvalidOperationException("unavailable"));
        AppSession session = Create(custody, prefs: prefs.Object);

        Func<Task> boot = session.BootAsync;
        await boot.Should().ThrowAsync<InvalidOperationException>();
        session.IsBooting.Should().BeFalse();
    }

    /// <summary>Denied custody authentication must never start a product session.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unlock_WhenCustodyDenies_DoesNotStartProductSession(bool deviceOwner)
    {
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.UnlockAsync("123456")).ReturnsAsync(false);
        custody.Setup(c => c.UnlockWithDeviceSecretAsync()).ReturnsAsync(false);
        Mock<IProductSessionCoordinator> product = new(MockBehavior.Strict);
        AppSession session = Create(custody, product.Object);

        bool unlocked = deviceOwner ? await session.UnlockWithDeviceOwnerAsync() : await session.UnlockAsync("123456");

        unlocked.Should().BeFalse();
        session.AccessToken.Should().BeNull();
        product.VerifyNoOtherCalls();
    }

    /// <summary>Device-owner unlock uses the same product startup and idle policy as PIN unlock.</summary>
    [Theory]
    [InlineData(0, 60000)]
    [InlineData(45, 45000)]
    public async Task DeviceOwnerUnlock_StartsProductSession(int seconds, int expectedMs)
    {
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.CanUnlockWithDeviceOwnerAsync()).ReturnsAsync(true);
        custody.Setup(c => c.UnlockWithDeviceSecretAsync()).ReturnsAsync(true);
        Mock<IProductSessionCoordinator> product = new();
        product.Setup(p => p.StartAsync(true, CancellationToken.None))
            .ReturnsAsync(new ProductSessionStartResult("session-token", seconds));
        AppSession session = Create(custody, product.Object);

        (await session.CanUnlockWithDeviceOwnerAsync()).Should().BeTrue();
        (await session.UnlockWithDeviceOwnerAsync()).Should().BeTrue();

        session.AccessToken.Should().Be("session-token");
        session.IdleMs.Should().Be(expectedMs);
        product.Verify(p => p.StartAsync(true, CancellationToken.None), Times.Once);
    }

    /// <summary>All recognized startup failures clear product state and relock custody.</summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("format")]
    [InlineData("argument")]
    [InlineData("sqlite")]
    [InlineData("database")]
    [InlineData("json")]
    [InlineData("http")]
    [InlineData("websocket")]
    [InlineData("canceled")]
    public async Task Unlock_WhenProductStartupFails_RollsBack(string failure)
    {
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.UnlockAsync("123456")).ReturnsAsync(true);
        Mock<IProductSessionCoordinator> product = new();
        product.Setup(p => p.StartAsync(true, CancellationToken.None)).ThrowsAsync(CreateFailure(failure));
        product.Setup(p => p.DisconnectAsync()).Returns(Task.CompletedTask);
        AppSession session = Create(custody, product.Object);

        (await session.UnlockAsync("123456")).Should().BeFalse();

        session.AccessToken.Should().BeNull();
        custody.Verify(c => c.Lock(), Times.Once);
        product.Verify(p => p.StopSession(), Times.Once);
        product.Verify(p => p.DisconnectAsync(), Times.Once);
    }

    /// <summary>Expected disconnect faults do not prevent a later unlock.</summary>
    [Theory]
    [InlineData("websocket")]
    [InlineData("disposed")]
    [InlineData("invalid")]
    [InlineData("canceled")]
    public async Task Lock_WhenDisconnectFaults_AllowsNextUnlock(string failure)
    {
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.UnlockAsync("123456")).ReturnsAsync(true);
        Mock<IProductSessionCoordinator> product = new();
        product.Setup(p => p.StartAsync(true, CancellationToken.None)).ReturnsAsync(new ProductSessionStartResult("next", 60));
        product.Setup(p => p.DisconnectAsync()).ThrowsAsync(CreateFailure(failure));
        AppSession session = Create(custody, product.Object);

        session.Lock();
        (await session.UnlockAsync("123456")).Should().BeTrue();

        session.AccessToken.Should().Be("next");
    }

    /// <summary>Touch resets idle accounting; the exact deadline locks only an unlocked session.</summary>
    [Fact]
    public async Task IdleDeadline_UsesLastTouchAndCustodyState()
    {
        Mock<ICustodyService> custody = new();
        custody.SetupGet(c => c.IsUnlocked).Returns(true);
        custody.Setup(c => c.UnlockAsync("123456")).ReturnsAsync(true);
        Mock<IProductSessionCoordinator> product = new();
        product.Setup(p => p.StartAsync(true, CancellationToken.None)).ReturnsAsync(new ProductSessionStartResult("token", 1));
        product.Setup(p => p.DisconnectAsync()).Returns(Task.CompletedTask);
        ManualClock clock = new();
        AppSession session = Create(custody, product.Object, clock: clock);
        await session.UnlockAsync("123456");
        int locked = 0;
        session.Locked += (_, _) => locked++;

        clock.Advance(TimeSpan.FromMilliseconds(999));
        session.CheckIdleAndMaybeLock().Should().BeFalse();
        session.Touch();
        clock.Advance(TimeSpan.FromMilliseconds(999));
        session.CheckIdleAndMaybeLock().Should().BeFalse();
        clock.Advance(TimeSpan.FromMilliseconds(1));
        session.CheckIdleAndMaybeLock().Should().BeTrue();
        session.AccessToken.Should().BeNull();
        locked.Should().Be(1);
        custody.SetupGet(c => c.IsUnlocked).Returns(false);
        session.CheckIdleAndMaybeLock().Should().BeFalse();
        custody.Verify(c => c.Lock(), Times.Once);
    }

    /// <summary>Custody expiry tears down an active product session and emits one notification.</summary>
    [Fact]
    public async Task CustodyLocked_ClearsTokenAndNotifiesOnlyOnce()
    {
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.UnlockAsync("123456")).ReturnsAsync(true);
        Mock<IProductSessionCoordinator> product = new();
        product.Setup(p => p.StartAsync(true, CancellationToken.None)).ReturnsAsync(new ProductSessionStartResult("token", 60));
        product.Setup(p => p.DisconnectAsync()).Returns(Task.CompletedTask);
        AppSession session = Create(custody, product.Object);
        await session.UnlockAsync("123456");
        int notifications = 0;
        session.Locked += (_, _) => notifications++;

        custody.Raise(c => c.Locked += null, EventArgs.Empty);
        custody.Raise(c => c.Locked += null, EventArgs.Empty);

        session.AccessToken.Should().BeNull();
        notifications.Should().Be(1);
        product.Verify(p => p.StopSession(), Times.Once);
        product.Verify(p => p.DisconnectAsync(), Times.Once);
    }

    private static AppSession Create(
        Mock<ICustodyService> custody,
        IProductSessionCoordinator? product = null,
        IPrefsStore? prefs = null,
        TimeProvider? clock = null)
        => new(
            custody.Object,
            product ?? Mock.Of<IProductSessionCoordinator>(),
            Mock.Of<ILocalWalletSeeder>(),
            prefs ?? Mock.Of<IPrefsStore>(),
            clock ?? new ManualClock());

    private static Exception CreateFailure(string failure) => failure switch
    {
        "invalid" => new InvalidOperationException("startup"),
        "format" => new FormatException("startup"),
        "argument" => new ArgumentException("startup"),
        "sqlite" => new SqliteException("startup", 1),
        "database" => new DbUpdateException("startup"),
        "json" => new JsonException("startup"),
        "http" => new HttpRequestException("startup"),
        "websocket" => new WebSocketException(),
        "disposed" => new ObjectDisposedException("stream"),
        "canceled" => new OperationCanceledException(),
        _ => throw new ArgumentOutOfRangeException(nameof(failure)),
    };

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }
}
