// <copyright file="PrioritizedJobDispatcherTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Persist;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class PrioritizedJobDispatcherTests
{
    [Fact]
    public async Task EnqueueAsync_OrdersPriorityThenFifo()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource blockerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseBlocker = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<string> order = [];

        _ = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                blockerStarted.SetResult();
                await releaseBlocker.Task.WaitAsync(ct);
            },
            CancellationToken.None);
        await blockerStarted.Task;

        _ = dispatcher.EnqueueAsync(
            SyncPriority.Background,
            _ =>
            {
                order.Add("background-first");
                return Task.CompletedTask;
            },
            CancellationToken.None);
        _ = dispatcher.EnqueueAsync(
            SyncPriority.Background,
            _ =>
            {
                order.Add("background-second");
                return Task.CompletedTask;
            },
            CancellationToken.None);
        _ = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ =>
            {
                order.Add("interactive");
                return Task.CompletedTask;
            },
            CancellationToken.None);

        releaseBlocker.SetResult();
        await dispatcher.DrainAsync(CancellationToken.None);

        order.Should().Equal("interactive", "background-first", "background-second");
    }

    [Fact]
    public async Task EnqueueAsync_CountsWholeAsyncOperationAgainstConcurrency()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task first = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                firstStarted.SetResult();
                await releaseFirst.Task.WaitAsync(ct);
            },
            CancellationToken.None);
        await firstStarted.Task;
        Task second = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ =>
            {
                secondStarted.SetResult();
                return Task.CompletedTask;
            },
            CancellationToken.None);

        secondStarted.Task.IsCompleted.Should().BeFalse();
        releaseFirst.SetResult();
        await Task.WhenAll(first, second);
        secondStarted.Task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task EnqueueAsync_PropagatesFaultAndDrainObservesIt()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        Task failed = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ => throw new NotSupportedException("quote failed"),
            CancellationToken.None);

        Func<Task> observeFailure = () => failed;
        await observeFailure.Should().ThrowAsync<NotSupportedException>();
        await dispatcher.DrainAsync(CancellationToken.None);
    }

    [Fact]
    public async Task EnqueueAsync_CallerCancellationReachesQueuedWork()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource blockerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseBlocker = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                blockerStarted.SetResult();
                await releaseBlocker.Task.WaitAsync(ct);
            },
            CancellationToken.None);
        await blockerStarted.Task;
        using CancellationTokenSource cancellation = new();
        Task canceled = dispatcher.EnqueueAsync(
            SyncPriority.Background,
            ct => Task.Delay(Timeout.InfiniteTimeSpan, ct),
            cancellation.Token);

        await cancellation.CancelAsync();
        releaseBlocker.SetResult();

        Func<Task> observeCancellation = () => canceled;
        await observeCancellation.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Dispose_CancelsRunningWork()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource startedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task running = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                startedSignal.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            },
            CancellationToken.None);
        await startedSignal.Task;

        dispatcher.Dispose();

        Func<Task> observeCancellation = () => running;
        await observeCancellation.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Dispose_CancelsQueuedWorkWithoutStartingIt()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource runningStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRunning = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int queuedRuns = 0;
        Task running = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async _ =>
            {
                runningStarted.SetResult();
                await releaseRunning.Task;
            },
            CancellationToken.None);
        await runningStarted.Task;
        Task queued = dispatcher.EnqueueAsync(
            SyncPriority.Background,
            _ =>
            {
                Interlocked.Increment(ref queuedRuns);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        dispatcher.Dispose();

        queued.IsCanceled.Should().BeTrue();
        releaseRunning.SetResult();
        await running;
        Volatile.Read(ref queuedRuns).Should().Be(0);
    }

    [Fact]
    public void EnqueueAsync_AfterDisposeThrows()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        dispatcher.Dispose();

        Action enqueue = () => dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ => Task.CompletedTask,
            CancellationToken.None);

        enqueue.Should().Throw<ObjectDisposedException>();
    }

    private static PrioritizedJobDispatcher CreateDispatcher() =>
        new(Options.Create(new SyncSchedulerOptions { MaxConcurrency = 1 }));
}
