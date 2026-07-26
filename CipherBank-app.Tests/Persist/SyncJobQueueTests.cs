// <copyright file="SyncJobQueueTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class SyncJobQueueTests
{
    [Fact]
    public async Task Enqueue_P2ThenP1_P1RunsBeforeWaitingP2_WhenConcurrencyAllows()
    {
        var queue = new SyncJobQueue();
        var order = new List<string>();
        var gate1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        queue.Enqueue("p2-a", SyncPriority.P2, async ct =>
        {
            lock (order)
            {
                order.Add("p2-a-start");
            }

            await gate1.Task.WaitAsync(ct).ConfigureAwait(false);
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

            await gate2.Task.WaitAsync(ct).ConfigureAwait(false);
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
        }).ConfigureAwait(false);

        queue.Enqueue("p2-c", SyncPriority.P2, async ct =>
        {
            lock (order)
            {
                order.Add("p2-c-start");
            }

            await Task.Delay(10, ct).ConfigureAwait(false);
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

            await Task.Delay(10, ct).ConfigureAwait(false);
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
        }).ConfigureAwait(false);

        lock (order)
        {
            order.IndexOf("p1-d-start").Should().BeLessThan(order.IndexOf("p2-c-start"));
        }

        gate2.SetResult();
        await queue.DrainAsync(default).ConfigureAwait(false);
    }

    [Fact]
    public async Task Enqueue_DuplicateKey_SkipsSecondWhileInFlight()
    {
        var queue = new SyncJobQueue();
        int runCount = 0;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        queue.Enqueue("btc", SyncPriority.P1, async ct =>
        {
            Interlocked.Increment(ref runCount);
            await gate.Task.WaitAsync(ct).ConfigureAwait(false);
        });

        await WaitUntilAsync(() => Volatile.Read(ref runCount) == 1).ConfigureAwait(false);

        queue.Enqueue("btc", SyncPriority.P1, async ct =>
        {
            Interlocked.Increment(ref runCount);
            await Task.CompletedTask.ConfigureAwait(false);
        });

        gate.SetResult();
        await queue.DrainAsync(default).ConfigureAwait(false);

        runCount.Should().Be(1);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 5000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!predicate())
        {
            if (Environment.TickCount64 >= deadline)
            {
                throw new TimeoutException("Condition was not met within the timeout.");
            }

            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}
