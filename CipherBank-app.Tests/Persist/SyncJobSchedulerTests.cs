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
        SyncJobScheduler queue = new SyncJobScheduler(
            TaskScheduler.Default,
            new SyncSchedulerOptions { MaxConcurrency = 2 });
        List<string> order = new List<string>();
        TaskCompletionSource gate1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
    public async Task Enqueue_DuplicateKey_SkipsSecondWhileInFlight()
    {
        SyncJobScheduler queue = new SyncJobScheduler();
        int runCount = 0;
        TaskCompletionSource gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
    public async Task Enqueue_DispatchesThroughInjectedTaskScheduler()
    {
        RecordingTaskScheduler taskScheduler = new RecordingTaskScheduler();
        SyncJobScheduler queue = new SyncJobScheduler(
            taskScheduler,
            new SyncSchedulerOptions { MaxConcurrency = 1 });
        int runs = 0;

        _ = queue.EnqueueAsync("btc", SyncPriority.Interactive, _ =>
        {
            runs++;
            return Task.CompletedTask;
        });
        await queue.DrainAsync(default);

        runs.Should().Be(1);
        taskScheduler.QueuedTasks.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EnqueueAsync_FaultedWork_ExposesFailureAndAllowsReenqueue()
    {
        SyncJobScheduler queue = new SyncJobScheduler();

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
        SyncJobScheduler queue = new SyncJobScheduler(
            TaskScheduler.Default,
            new SyncSchedulerOptions { MaxConcurrency = 1 });
        TaskCompletionSource blocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = queue.EnqueueAsync("blocker", SyncPriority.Interactive, ct => blocker.Task.WaitAsync(ct));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
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
        SyncJobScheduler queue = new SyncJobScheduler();
        Task started = queue.EnqueueAsync(
            "running",
            SyncPriority.Interactive,
            async ct => await Task.Delay(Timeout.InfiniteTimeSpan, ct));

        queue.Dispose();

        Func<Task> observeCancellation = () => started;
        await observeCancellation.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Dispose_QueuedJob_IsCanceledAndNeverStarts()
    {
        SyncJobScheduler queue = new SyncJobScheduler(
            TaskScheduler.Default,
            new SyncSchedulerOptions { MaxConcurrency = 1 });
        TaskCompletionSource blocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int queuedRuns = 0;

        // Occupies the single slot and ignores its cancellation token so it can
        // finish after Dispose, exercising the completion-time dispatch path.
        Task running = queue.EnqueueAsync("running", SyncPriority.Interactive, _ => blocker.Task);
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
        SyncJobScheduler queue = new SyncJobScheduler();
        queue.Dispose();

        Action enqueue = () => queue.EnqueueAsync("late", SyncPriority.Interactive, _ => Task.CompletedTask);
        enqueue.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Unset_max_concurrency_resolves_to_half_processor_count()
    {
        SyncSchedulerOptions options = new SyncSchedulerOptions();
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

    private sealed class RecordingTaskScheduler : TaskScheduler
    {
        public int QueuedTasks { get; private set; }

        protected override IEnumerable<Task>? GetScheduledTasks() => Array.Empty<Task>();

        protected override void QueueTask(Task task)
        {
            QueuedTasks++;
            TryExecuteTask(task).Should().BeTrue();
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
    }
}
