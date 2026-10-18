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
    public async Task DrainAsync_AfterSynchronouslyObservedFaultDoesNotThrow()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        Task failed = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ => throw new NotSupportedException("quote failed"),
            CancellationToken.None);

        Task observation = failed.ContinueWith(
            static _ => (object?)null,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        await observation;
        failed.IsFaulted.Should().BeTrue();

        Func<Task> drain = () => dispatcher.DrainAsync(CancellationToken.None);
        await drain.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DrainAsync_DoesNotRethrowWhenFaultCompletesDuringDrain()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource workStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFault = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task failed = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                workStarted.SetResult();
                await releaseFault.Task.WaitAsync(ct).ConfigureAwait(false);
                throw new NotSupportedException("quote failed");
            },
            CancellationToken.None);
        await workStarted.Task;

        Task drain = dispatcher.DrainAsync(CancellationToken.None);
        releaseFault.SetResult();

        Func<Task> observeFailure = () => failed;
        await observeFailure.Should().ThrowAsync<NotSupportedException>();

        Func<Task> observeDrain = () => drain;
        await observeDrain.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DrainAsync_WaitsForRunningWorkBeforeReturning()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource runningStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRunning = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int completionOrder = 0;

        _ = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                runningStarted.SetResult();
                await releaseRunning.Task.WaitAsync(ct).ConfigureAwait(false);
                Interlocked.Exchange(ref completionOrder, 1);
            },
            CancellationToken.None);
        await runningStarted.Task;

        Task drain = dispatcher.DrainAsync(CancellationToken.None);
        drain.IsCompleted.Should().BeFalse();

        releaseRunning.SetResult();
        await drain;

        Volatile.Read(ref completionOrder).Should().Be(1);
    }

    [Fact]
    public async Task DrainAsync_IncludesWorkEnqueuedDuringDrain()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int secondRuns = 0;

        _ = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                firstStarted.SetResult();
                await releaseFirst.Task.WaitAsync(ct).ConfigureAwait(false);
            },
            CancellationToken.None);
        await firstStarted.Task;

        Task drain = dispatcher.DrainAsync(CancellationToken.None);
        Task second = dispatcher.EnqueueAsync(
            SyncPriority.Background,
            _ =>
            {
                Interlocked.Increment(ref secondRuns);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        releaseFirst.SetResult();
        await drain;
        await second;

        Volatile.Read(ref secondRuns).Should().Be(1);
    }

    [Fact]
    public async Task DrainAsync_DoesNotRethrowMultipleObservedFaults()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher(maxConcurrency: 2);
        Task first = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ => throw new InvalidOperationException("first"),
            CancellationToken.None);
        Task second = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ => throw new InvalidOperationException("second"),
            CancellationToken.None);

        Func<Task> observeFirst = () => first;
        Func<Task> observeSecond = () => second;
        await observeFirst.Should().ThrowAsync<InvalidOperationException>();
        await observeSecond.Should().ThrowAsync<InvalidOperationException>();

        Func<Task> drain = () => dispatcher.DrainAsync(CancellationToken.None);
        await drain.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DrainAsync_ReturnsPromptlyWhenEnqueuedWorkAlreadyCompleted()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        Task completed = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ => Task.CompletedTask,
            CancellationToken.None);
        await completed;

        Task drain = dispatcher.DrainAsync(CancellationToken.None);
        await drain.WaitAsync(TimeSpan.FromSeconds(1));

        drain.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task DrainAsync_WaitsForRemainingWorkWhenSomeJobsAlreadyCompleted()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher(maxConcurrency: 2);
        TaskCompletionSource slowStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseSlow = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int slowCompletionOrder = 0;

        Task fast = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ => Task.CompletedTask,
            CancellationToken.None);
        Task slow = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                slowStarted.SetResult();
                await releaseSlow.Task.WaitAsync(ct).ConfigureAwait(false);
                Interlocked.Exchange(ref slowCompletionOrder, 1);
            },
            CancellationToken.None);
        await slowStarted.Task;
        await fast;

        Task drain = dispatcher.DrainAsync(CancellationToken.None);
        drain.IsCompleted.Should().BeFalse();

        releaseSlow.SetResult();
        await drain.WaitAsync(TimeSpan.FromSeconds(1));

        Volatile.Read(ref slowCompletionOrder).Should().Be(1);
        await slow;
    }

    [Fact]
    public async Task DrainAsync_DoesNotHangWhenFaultedWorkRemainsRegistered()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        Task failed = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            _ => throw new InvalidOperationException("faulted before drain"),
            CancellationToken.None);

        Func<Task> observeFailure = () => failed;
        await observeFailure.Should().ThrowAsync<InvalidOperationException>();

        Task drain = dispatcher.DrainAsync(CancellationToken.None);
        await drain.WaitAsync(TimeSpan.FromSeconds(1));

        drain.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task DrainAsync_DoesNotHangWhenJobCompletesDuringDrain()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource workStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseWork = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task work = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                workStarted.SetResult();
                await releaseWork.Task.WaitAsync(ct).ConfigureAwait(false);
            },
            CancellationToken.None);
        await workStarted.Task;

        Task drain = dispatcher.DrainAsync(CancellationToken.None);
        releaseWork.SetResult();

        await drain.WaitAsync(TimeSpan.FromSeconds(1));
        await work;

        drain.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task DrainAsync_AfterDisposeReturnsWithoutWaitingForRunningWork()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource runningStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRunning = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task running = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async _ =>
            {
                runningStarted.SetResult();
                await releaseRunning.Task.ConfigureAwait(false);
            },
            CancellationToken.None);
        await runningStarted.Task;

        dispatcher.Dispose();

        Func<Task> drain = () => dispatcher.DrainAsync(CancellationToken.None);
        await drain.Should().NotThrowAsync();

        releaseRunning.SetResult();
        await running;
    }

    [Fact]
    public void Dispose_CalledTwiceDoesNotThrow()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        dispatcher.Dispose();

        Action secondDispose = () => dispatcher.Dispose();
        secondDispose.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_DoesNotHangWhenWorkerDrainsDequeuedUnstartedWork()
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
                await releaseRunning.Task.ConfigureAwait(false);
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

        Func<Task> observeRunning = () => running.WaitAsync(TimeSpan.FromSeconds(1));
        await observeRunning.Should().NotThrowAsync();

        Volatile.Read(ref queuedRuns).Should().Be(0);
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

    private static PrioritizedJobDispatcher CreateDispatcher(int maxConcurrency = 1) =>
        new(Options.Create(new SyncSchedulerOptions { MaxConcurrency = maxConcurrency }));
}
