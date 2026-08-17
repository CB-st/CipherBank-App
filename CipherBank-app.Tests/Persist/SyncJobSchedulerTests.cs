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
        SyncJobScheduler queue = new SyncJobScheduler();
        List<string> order = new List<string>();
        TaskCompletionSource gate1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource gate2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        queue.Enqueue("p2-a", SyncPriority.P2, async ct =>
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
        queue.Enqueue("p2-b", SyncPriority.P2, async ct =>
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

        queue.Enqueue("p2-c", SyncPriority.P2, async ct =>
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
        queue.Enqueue("p1-d", SyncPriority.P1, async ct =>
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

        queue.Enqueue("btc", SyncPriority.P1, async ct =>
        {
            Interlocked.Increment(ref runCount);
            await gate.Task.WaitAsync(ct);
        });

        await WaitUntilAsync(() => Volatile.Read(ref runCount) == 1);

        queue.Enqueue("btc", SyncPriority.P1, async ct =>
        {
            Interlocked.Increment(ref runCount);
            await Task.CompletedTask;
        });

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

        queue.Enqueue("btc", SyncPriority.P1, _ =>
        {
            runs++;
            return Task.CompletedTask;
        });
        await queue.DrainAsync(default);

        runs.Should().Be(1);
        taskScheduler.QueuedTasks.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Default_max_concurrency_is_clamped_processor_count()
    {
        int derived = SyncSchedulerOptions.DeriveDefaultMaxConcurrency();
        derived.Should().BeInRange(
            SyncSchedulerOptions.MinConcurrency,
            SyncSchedulerOptions.DefaultMaxConcurrencyCap);
        new SyncSchedulerOptions().MaxConcurrency.Should().Be(derived);
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
