// <copyright file="ProductSessionCoordinatorTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using CipherBank_app.Session;
using CipherBank_app.V1;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Session;

public class ProductSessionCoordinatorTests
{
    [Fact]
    public async Task StartAsync_PreservesLocalIdleTimeoutWhenRefreshFails()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-sess-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(new FileInfo(path));
        await db.InitializeAsync();
        PrefsStore prefs = new PrefsStore(db);
        UserPrefs local = await prefs.LoadAsync();
        local.LockIdleSeconds = 180;
        await prefs.SaveAsync(local);

        Mock<IProductClient> client = new Mock<IProductClient>(MockBehavior.Strict);
        client.Setup(c => c.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDto { AccessToken = "tok", RefreshToken = "ref" });

        Mock<IStreamService> stream = new Mock<IStreamService>(MockBehavior.Strict);
        stream.Setup(s => s.ConnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IStreamHub> hub = new Mock<IStreamHub>(MockBehavior.Strict);
        bool started = false;
        hub.Setup(h => h.Start()).Callback(() => started = true);
        hub.Setup(h => h.StopStreaming());

        Mock<IPrefsSyncService> prefsSync = new Mock<IPrefsSyncService>(MockBehavior.Strict);
        prefsSync.Setup(p => p.PullMergeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));

        Mock<IAccountBootstrapService> bootstrap = new Mock<IAccountBootstrapService>(MockBehavior.Strict);
        InMemoryProductSessionStore sessions = new InMemoryProductSessionStore();

        ProductSessionCoordinator coordinator = new ProductSessionCoordinator(
            client.Object,
            stream.Object,
            hub.Object,
            prefs,
            prefsSync.Object,
            bootstrap.Object,
            sessions);

        ProductSessionStartResult result = await coordinator.StartAsync(applyBootstrap: false, CancellationToken.None);
        result.LockIdleSeconds.Should().Be(180);
        started.Should().BeTrue();
        stream.Verify(s => s.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_StopsHubWhenConnectFails()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-sess-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(new FileInfo(path));
        await db.InitializeAsync();
        PrefsStore prefs = new PrefsStore(db);

        Mock<IProductClient> client = new Mock<IProductClient>(MockBehavior.Strict);
        client.Setup(c => c.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionDto { AccessToken = "tok", RefreshToken = "ref" });

        Mock<IStreamService> stream = new Mock<IStreamService>(MockBehavior.Strict);
        stream.Setup(s => s.ConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connect failed"));

        Mock<IStreamHub> hub = new Mock<IStreamHub>(MockBehavior.Strict);
        hub.Setup(h => h.Start());
        hub.Setup(h => h.StopStreaming());

        ProductSessionCoordinator coordinator = new ProductSessionCoordinator(
            client.Object,
            stream.Object,
            hub.Object,
            prefs,
            Mock.Of<IPrefsSyncService>(),
            Mock.Of<IAccountBootstrapService>(),
            new InMemoryProductSessionStore());

        Func<Task> act = () => coordinator.StartAsync(false, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
        hub.Verify(h => h.Start(), Times.Once);
        hub.Verify(h => h.StopStreaming(), Times.Once);
    }
}
