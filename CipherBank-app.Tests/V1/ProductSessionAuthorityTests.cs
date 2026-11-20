// <copyright file="ProductSessionAuthorityTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class ProductSessionAuthorityTests
{
    [Fact]
    public void For_SharesAuthorityPerStore_AndRejectsNull()
    {
        InMemoryProductSessionStore store = new();
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store);
        using ProductSessionAuthority other = ProductSessionAuthority.For(new InMemoryProductSessionStore());

        ProductSessionAuthority.For(store).Should().BeSameAs(authority);
        other.Should().NotBeSameAs(authority);
        Action missing = () => ProductSessionAuthority.For(null!);
        missing.Should().Throw<ArgumentNullException>().WithParameterName("backing");
    }

    [Fact]
    public void Commit_PublishesOnlyForCurrentOwner_AndRejectsNull()
    {
        using ProductSessionAuthority authority = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        using ProductSessionAuthority other = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        ProductSessionOperation original = authority.Capture();
        int publications = 0;

        authority.Check(original);
        authority.Commit(original, () => publications++);

        Action missing = () => authority.Commit(original, null!);
        missing.Should().Throw<ArgumentNullException>().WithParameterName("publish");
        Action foreign = () => authority.Commit(other.Capture(), () => publications++);
        foreign.Should().Throw<OperationCanceledException>();

        authority.Begin();
        Action stale = () => authority.Commit(original, () => publications++);
        stale.Should().Throw<OperationCanceledException>();
        original.Token.IsCancellationRequested.Should().BeTrue();
        publications.Should().Be(1);
    }

    [Fact]
    public async Task Invalidate_IgnoresStaleAndForeignOwners_ThenClearsCurrentSession()
    {
        InMemoryProductSessionStore store = new();
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store);
        using ProductSessionAuthority other = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        ProductSessionOperation stale = authority.Capture();
        ProductSessionOperation current = authority.Begin();
        ProductSessionSnapshot snapshot = await authority.PublishAsync(current, Session(), CancellationToken.None);
        int callbacks = 0;

        authority.Invalidate(stale);
        authority.Invalidate(other.Capture(), () => callbacks++);
        (await authority.ReadAsync(current)).Should().BeSameAs(snapshot);
        callbacks.Should().Be(0);
        current.Token.IsCancellationRequested.Should().BeFalse();

        authority.Invalidate(current, () =>
        {
            authority.Invalidate();
            callbacks++;
        });
        await authority.Drain;
        callbacks.Should().Be(1);
        current.Token.IsCancellationRequested.Should().BeTrue();
        (await store.GetAsync()).Should().BeNull();
        (await authority.ReadAsync(authority.Capture())).Should().BeNull();
        Func<Task> oldRead = () => authority.ReadAsync(current);
        await oldRead.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Invalidate_ReentrantCallback_ClearsOnceEvenWhenCallbackThrows()
    {
        Mock<IProductSessionStore> store = new(MockBehavior.Strict);
        store.Setup(s => s.Clear());
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store.Object);
        ProductSessionOperation operation = authority.Capture();
        InvalidOperationException failure = new("callback failed");
        int nestedCallbacks = 0;
        Task? observedDrain = null;

        Action revoke = () => authority.Invalidate(() =>
        {
            observedDrain = authority.Drain;
            authority.Invalidate(() => nestedCallbacks++);
            throw failure;
        });
        revoke.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(failure);
        await authority.Drain;

        observedDrain.Should().BeSameAs(authority.Drain);
        nestedCallbacks.Should().Be(1);
        operation.Token.IsCancellationRequested.Should().BeTrue();
        (await authority.ReadAsync(authority.Capture())).Should().BeNull();
        store.Verify(s => s.Clear(), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_RejectsRevisionConflict_WithoutChangingStoredSnapshot()
    {
        InMemoryProductSessionStore store = new();
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store);
        ProductSessionOperation operation = authority.Begin();
        ProductSessionSnapshot first = await authority.PublishAsync(operation, Session(), CancellationToken.None);
        ProductSessionSnapshot second = await authority.PublishAsync(operation, Session("second"), first.Revision, CancellationToken.None);

        Func<Task> conflict = () => authority.PublishAsync(operation, Session("stale"), first.Revision, CancellationToken.None);
        await conflict.Should().ThrowAsync<OperationCanceledException>().WithMessage("Credential revision changed.");
        (await authority.ReadAsync(operation)).Should().BeSameAs(second);
        second.Revision.Should().Be(first.Revision + 1);
        second.Access.Should().Be("second");
        second.Refresh.Should().Be("refresh-second");
        second.Expires.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(Session().ExpiresAt));
        (await store.GetAsync())!.Value.Access.Should().Be("second");
    }

    [Fact]
    public async Task PublishAsync_SaveFailure_ClearsAndHidesPartialCredentials()
    {
        Mock<IProductSessionStore> store = new(MockBehavior.Strict);
        InvalidOperationException failure = new("save failed");
        store.Setup(s => s.SaveAsync(It.IsAny<SessionDto>())).ThrowsAsync(failure);
        store.Setup(s => s.Clear());
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store.Object);
        ProductSessionOperation operation = authority.Begin();

        Func<Task> publish = () => authority.PublishAsync(operation, Session(), CancellationToken.None);

        (await publish.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        (await authority.ReadAsync(operation)).Should().BeNull();
        store.Verify(s => s.Clear(), Times.Once);
        await authority.Drain;
        authority.Drain.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_SaveAndClearFailure_FaultsDrainAndBlocksQueuedEffects()
    {
        Mock<IProductSessionStore> store = new(MockBehavior.Strict);
        InvalidOperationException clearError = new("clear failed");
        store.Setup(s => s.Clear()).Throws(clearError);
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store.Object);
        ProductSessionOperation operation = authority.Begin();
        List<Task> queued = [];
        int localEffects = 0;
        store.Setup(s => s.SaveAsync(It.IsAny<SessionDto>())).Returns(() =>
        {
            // Both callers pass the initial drain check while this save owns the lane.
            queued.Add(authority.PublishAsync(operation, Session("blocked"), CancellationToken.None));
            queued.Add(authority.RunLocalAsync(operation, () => Task.FromResult(++localEffects), CancellationToken.None));
            return Task.FromException(new InvalidOperationException("save failed"));
        });

        Func<Task> publish = () => authority.PublishAsync(operation, Session(), CancellationToken.None);

        (await publish.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(clearError);
        (await authority.ReadAsync(operation)).Should().BeNull();
        queued.Should().HaveCount(2);
        foreach (Task pending in queued)
        {
            Func<Task> blocked = () => pending;
            await blocked.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cleanup must recover*");
        }

        localEffects.Should().Be(0);
        store.Verify(s => s.Clear(), Times.Once);
        store.Verify(s => s.SaveAsync(It.IsAny<SessionDto>()), Times.Once);
        Func<Task> drain = () => authority.Drain;
        (await drain.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(clearError);
    }

    [Fact]
    public async Task Invalidate_ClearFailure_BlocksEffectsUntilSuccessfulRetry()
    {
        Mock<IProductSessionStore> store = new(MockBehavior.Strict);
        InvalidOperationException failure = new("clear failed");
        store.SetupSequence(s => s.Clear()).Throws(failure).Pass();
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store.Object);

        authority.Invalidate();
        Func<Task> drain = () => authority.Drain;
        (await drain.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        bool ran = false;
        Func<Task> local = () => authority.RunLocalAsync(
            authority.Capture(), () => Task.FromResult(ran = true), CancellationToken.None);
        Func<Task> publish = () => authority.PublishAsync(authority.Capture(), Session(), CancellationToken.None);

        await local.Should().ThrowAsync<InvalidOperationException>();
        await publish.Should().ThrowAsync<InvalidOperationException>();
        ran.Should().BeFalse();
        store.Verify(s => s.SaveAsync(It.IsAny<SessionDto>()), Times.Never);

        authority.Invalidate();
        await authority.Drain;
        await local();
        ran.Should().BeTrue();
        store.Verify(s => s.Clear(), Times.Exactly(2));
    }

    [Fact]
    public async Task RunLocalAsync_RejectsNullAndRevocationDuringAction_ThenReleasesLane()
    {
        using ProductSessionAuthority authority = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        ProductSessionOperation operation = authority.Begin();

        Task missing = authority.RunLocalAsync(operation, null!, CancellationToken.None);
        missing.IsFaulted.Should().BeTrue();
        Func<Task> missingAction = () => missing;
        await missingAction.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
        Func<Task> revoked = () => authority.RunLocalAsync(
            operation,
            () =>
            {
                authority.Invalidate();
                return Task.CompletedTask;
            },
            CancellationToken.None);
        await revoked.Should().ThrowAsync<OperationCanceledException>();
        await authority.Drain;
        ProductSessionSnapshot replacement = await authority.PublishAsync(authority.Begin(), Session("replacement"), CancellationToken.None);
        replacement.Access.Should().Be("replacement");
    }

    [Fact]
    public async Task ReadAsync_ConcurrentInitialReaders_ShareOneBackingRead()
    {
        Mock<IProductSessionStore> store = new(MockBehavior.Strict);
        TaskCompletionSource<(string Access, string Refresh, DateTimeOffset Expires)?> read = new(TaskCreationOptions.RunContinuationsAsynchronously);
        store.Setup(s => s.GetAsync()).Returns(read.Task);
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store.Object);
        ProductSessionOperation operation = authority.Capture();

        Task<ProductSessionSnapshot?> first = authority.ReadAsync(operation);
        Task<ProductSessionSnapshot?> second = authority.ReadAsync(operation);
        read.SetResult(("access", "refresh", DateTimeOffset.UnixEpoch));
        ProductSessionSnapshot?[] snapshots = await Task.WhenAll(first, second);

        snapshots[0]!.Access.Should().Be("access");
        snapshots[0]!.Refresh.Should().Be("refresh");
        snapshots[0]!.Expires.Should().Be(DateTimeOffset.UnixEpoch);
        snapshots[1].Should().BeSameAs(snapshots[0]);
        store.Verify(s => s.GetAsync(), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_CancelledDuringSave_ClearsCredentialsBeforeReturning()
    {
        InMemoryProductSessionStore backing = new();
        using CancellationTokenSource cancellation = new();
        Mock<IProductSessionStore> store = new(MockBehavior.Strict);
        store.Setup(s => s.SaveAsync(It.IsAny<SessionDto>())).Returns(async (SessionDto session) =>
        {
            await backing.SaveAsync(session);
            cancellation.Cancel();
        });
        store.Setup(s => s.Clear()).Callback(backing.Clear);
        using ProductSessionAuthority authority = ProductSessionAuthority.For(store.Object);
        ProductSessionOperation operation = authority.Begin();

        Func<Task> publish = () => authority.PublishAsync(operation, Session(), cancellation.Token);
        await publish.Should().ThrowAsync<OperationCanceledException>();
        (await backing.GetAsync()).Should().BeNull();
        (await authority.ReadAsync(operation)).Should().BeNull();
        store.Verify(s => s.Clear(), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_FailureCanRetry_AndStaleSnapshotReusesNewCredentials()
    {
        using ProductSessionAuthority authority = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        ProductSessionSnapshot original = await authority.PublishAsync(authority.Begin(), Session(), CancellationToken.None);
        InvalidOperationException failure = new("refresh failed");

        Func<Task> failedRefresh = () => authority.RefreshAsync(original, _ => Task.FromException<SessionDto>(failure), CancellationToken.None);
        (await failedRefresh.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);
        (await authority.ReadAsync(original.Operation)).Should().BeSameAs(original);

        ProductSessionSnapshot refreshed = await authority.RefreshAsync(original, _ => Task.FromResult(Session("rotated")), CancellationToken.None);
        int unnecessaryRequests = 0;
        ProductSessionSnapshot reused = await authority.RefreshAsync(
            original,
            _ =>
            {
                unnecessaryRequests++;
                return Task.FromResult(Session("unexpected"));
            },
            CancellationToken.None);

        refreshed.Access.Should().Be("rotated");
        refreshed.Revision.Should().Be(original.Revision + 1);
        reused.Should().BeSameAs(refreshed);
        unnecessaryRequests.Should().Be(0);
    }

    [Fact]
    public async Task RefreshAsync_CancellingOneWaiter_DoesNotCancelOtherWaiter()
    {
        using ProductSessionAuthority authority = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        ProductSessionSnapshot original = await authority.PublishAsync(authority.Begin(), Session(), CancellationToken.None);
        TaskCompletionSource<SessionDto> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource cancellation = new();
        CancellationToken remoteToken = CancellationToken.None;
        int requests = 0;
        Task<SessionDto> Refresh(CancellationToken token)
        {
            requests++;
            remoteToken = token;
            return response.Task;
        }

        Task<ProductSessionSnapshot> cancelled = authority.RefreshAsync(original, Refresh, cancellation.Token);
        Task<ProductSessionSnapshot> surviving = authority.RefreshAsync(original, Refresh, CancellationToken.None);
        cancellation.Cancel();
        Func<Task> cancelledWaiter = () => cancelled;
        await cancelledWaiter.Should().ThrowAsync<OperationCanceledException>();
        remoteToken.IsCancellationRequested.Should().BeFalse();
        response.SetResult(Session("rotated"));
        (await surviving).Access.Should().Be("rotated");
        requests.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_LastWaiterCancelsRemoteWork_AndAllowsRetry()
    {
        using ProductSessionAuthority authority = ProductSessionAuthority.For(new InMemoryProductSessionStore());
        ProductSessionSnapshot original = await authority.PublishAsync(authority.Begin(), Session(), CancellationToken.None);
        using CancellationTokenSource cancellation = new();
        CancellationToken remoteToken = CancellationToken.None;
        Task<SessionDto> Refresh(CancellationToken token)
        {
            remoteToken = token;
            cancellation.Cancel();
            return Task.FromCanceled<SessionDto>(token);
        }

        Func<Task> cancelled = () => authority.RefreshAsync(original, Refresh, cancellation.Token);
        await cancelled.Should().ThrowAsync<OperationCanceledException>();
        remoteToken.IsCancellationRequested.Should().BeTrue();
        (await authority.ReadAsync(original.Operation)).Should().BeSameAs(original);
        ProductSessionSnapshot retry = await authority.RefreshAsync(original, _ => Task.FromResult(Session("retry")), CancellationToken.None);
        retry.Access.Should().Be("retry");
        retry.Revision.Should().Be(original.Revision + 1);
    }

    private static SessionDto Session(string access = "access") => new()
    {
        AccessToken = access,
        RefreshToken = "refresh-" + access,
        ExpiresAt = 1_800_000_000_000,
    };
}
