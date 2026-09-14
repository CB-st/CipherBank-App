// <copyright file="SyncJobSchedulerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class SyncJobSchedulerTests
{
    [Fact]
    public async Task Enqueue_P2ThenP1_P1RunsBeforeWaitingP2_WhenConcurrencyAllows()
    {
        using SyncJobScheduler queue = new(
            new SyncSchedulerOptions { MaxConcurrency = 2 });
        List<string> order = new();
        TaskCompletionSource gate1 = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate2 = new(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = queue.EnqueueAsync("p2-a", SyncPriority.Background, async ct =>
        {
            lock (order)
            {
                order.Add("p2-a-start");
            }

            await gate1.Task.WaitAsync(ct);
            lock (order)
            {
                order.Add("p2-a-end");
            }
        });
        _ = queue.EnqueueAsync("p2-b", SyncPriority.Background, async ct =>
        {
            lock (order)
            {
                order.Add("p2-b-start");
            }

            await gate2.Task.WaitAsync(ct);
            lock (order)
            {
                order.Add("p2-b-end");
            }
        });

        await WaitUntilAsync(() =>
        {
            lock (order)
            {
                return order.Count >= 2;
            }
        });

        _ = queue.EnqueueAsync("p2-c", SyncPriority.Background, async ct =>
        {
            lock (order)
            {
                order.Add("p2-c-start");
            }

            await Task.Delay(10, ct);
            lock (order)
            {
                order.Add("p2-c-end");
            }
        });
        _ = queue.EnqueueAsync("p1-d", SyncPriority.Interactive, async ct =>
        {
            lock (order)
            {
                order.Add("p1-d-start");
            }

            await Task.Delay(10, ct);
            lock (order)
            {
                order.Add("p1-d-end");
            }
        });

        gate1.SetResult();
        await WaitUntilAsync(() =>
        {
            lock (order)
            {
                return order.Contains("p1-d-start") && order.Contains("p2-c-start");
            }
        });

        lock (order)
        {
            order.IndexOf("p1-d-start").Should().BeLessThan(order.IndexOf("p2-c-start"));
        }

        gate2.SetResult();
        await queue.DrainAsync(default);
    }

    [Fact]
    public async Task Enqueue_SamePriority_PreservesFifoOrder()
    {
        using SyncJobScheduler queue = new(
            new SyncSchedulerOptions { MaxConcurrency = 1 });
        List<string> order = new();
        TaskCompletionSource blockerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseBlocker = new(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = queue.EnqueueAsync("blocker", SyncPriority.Interactive, async ct =>
        {
            blockerStarted.SetResult();
            await releaseBlocker.Task.WaitAsync(ct);
        });
        await blockerStarted.Task;

        _ = queue.EnqueueAsync("first", SyncPriority.Background, _ =>
        {
            order.Add("first");
            return Task.CompletedTask;
        });
        _ = queue.EnqueueAsync("second", SyncPriority.Background, _ =>
        {
            order.Add("second");
            return Task.CompletedTask;
        });

        releaseBlocker.SetResult();
        await queue.DrainAsync(default);

        order.Should().Equal("first", "second");
    }

    [Fact]
    public async Task Enqueue_DuplicateKey_SkipsSecondWhileInFlight()
    {
        using SyncJobScheduler queue = new();
        int runCount = 0;
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task first = queue.EnqueueAsync("btc", SyncPriority.Interactive, async ct =>
        {
            Interlocked.Increment(ref runCount);
            await gate.Task.WaitAsync(ct);
        });

        await WaitUntilAsync(() => Volatile.Read(ref runCount) == 1);

        Task duplicate = queue.EnqueueAsync("btc", SyncPriority.Interactive, async ct =>
        {
            Interlocked.Increment(ref runCount);
            await Task.CompletedTask;
        });

        duplicate.Should().BeSameAs(first);
        gate.SetResult();
        await queue.DrainAsync(default);

        runCount.Should().Be(1);
    }

    [Fact]
    public async Task Enqueue_MaxConcurrencyCountsWholeAsyncOperation()
    {
        using SyncJobScheduler queue = new(
            new SyncSchedulerOptions { MaxConcurrency = 1 });
        TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Task first = queue.EnqueueAsync("first", SyncPriority.Interactive, async ct =>
        {
            firstStarted.SetResult();
            await releaseFirst.Task.WaitAsync(ct);
        });
        await firstStarted.Task;

        Task second = queue.EnqueueAsync("second", SyncPriority.Interactive, _ =>
        {
            secondStarted.SetResult();
            return Task.CompletedTask;
        });

        secondStarted.Task.IsCompleted.Should().BeFalse();
        releaseFirst.SetResult();
        await Task.WhenAll(first, second);
        secondStarted.Task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task EnqueueAsync_FaultedWork_ExposesFailureAndAllowsReenqueue()
    {
        using SyncJobScheduler queue = new();

        Task failed = queue.EnqueueAsync(
            "btc",
            SyncPriority.Interactive,
            _ => throw new NotSupportedException("quote failed"));

        Func<Task> observeFailure = () => failed;
        await observeFailure.Should().ThrowAsync<NotSupportedException>();

        Task retry = queue.EnqueueAsync("btc", SyncPriority.Interactive, _ => Task.CompletedTask);
        await retry;
    }

    [Fact]
    public async Task EnqueueAsync_CallerCancellation_ReachesQueuedWork()
    {
        using SyncJobScheduler queue = new(
            new SyncSchedulerOptions { MaxConcurrency = 1 });
        TaskCompletionSource blocker = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = queue.EnqueueAsync("blocker", SyncPriority.Interactive, ct => blocker.Task.WaitAsync(ct));
        using CancellationTokenSource cancellation = new();
        Task canceled = queue.EnqueueAsync(
            "queued",
            SyncPriority.Background,
            async ct => await Task.Delay(Timeout.InfiniteTimeSpan, ct),
            cancellation.Token);

        await cancellation.CancelAsync();
        blocker.SetResult();

        Func<Task> observeCancellation = () => canceled;
        await observeCancellation.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Dispose_CancelsRunningWork()
    {
        using SyncJobScheduler queue = new();
        TaskCompletionSource startedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task started = queue.EnqueueAsync(
            "running",
            SyncPriority.Interactive,
            async ct =>
            {
                startedSignal.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            });
        await startedSignal.Task;

        queue.Dispose();

        Func<Task> observeCancellation = () => started;
        await observeCancellation.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Dispose_QueuedJob_IsCanceledAndNeverStarts()
    {
        using SyncJobScheduler queue = new(
            new SyncSchedulerOptions { MaxConcurrency = 1 });
        TaskCompletionSource runningStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource blocker = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int queuedRuns = 0;

        // Occupies the single slot and ignores its cancellation token so it can
        // finish after Dispose, exercising the completion-time dispatch path.
        Task running = queue.EnqueueAsync("running", SyncPriority.Interactive, async _ =>
        {
            runningStarted.SetResult();
            await blocker.Task;
        });
        await runningStarted.Task;
        Task queued = queue.EnqueueAsync("queued", SyncPriority.Background, _ =>
        {
            Interlocked.Increment(ref queuedRuns);
            return Task.CompletedTask;
        });

        queue.Dispose();

        // Queued-but-unstarted work completes as canceled synchronously at disposal.
        queued.IsCanceled.Should().BeTrue();
        Func<Task> observeQueuedCancellation = () => queued;
        await observeQueuedCancellation.Should().ThrowAsync<TaskCanceledException>();

        // The running job finishing after disposal must not dispatch drained work.
        blocker.SetResult();
        await running;
        Volatile.Read(ref queuedRuns).Should().Be(0);
    }

    [Fact]
    public void Enqueue_AfterDispose_Throws()
    {
        using SyncJobScheduler queue = new();
        queue.Dispose();

        Action enqueue = () => queue.EnqueueAsync("late", SyncPriority.Interactive, _ => Task.CompletedTask);
        enqueue.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Unset_max_concurrency_resolves_to_half_processor_count()
    {
        SyncSchedulerOptions options = new();
        options.MaxConcurrency.Should().Be(0);
        options.Resolve().Should().Be(
            Math.Clamp(
                (int)Math.Ceiling(Environment.ProcessorCount / 2.0),
                SyncSchedulerOptions.MinConcurrency,
                SyncSchedulerOptions.MaxAllowedConcurrency));
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 5000)
    {
        long deadline = Environment.TickCount64 + timeoutMs;
        while (!predicate())
        {
            if (Environment.TickCount64 >= deadline)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }

            await Task.Delay(10);
        }
    }
}
