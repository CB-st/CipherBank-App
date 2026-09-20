// <copyright file="SyncJobSchedulerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Concurrent;
using CipherBank_app.Configuration;
using CipherBank_app.Persist;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public sealed class SyncJobSchedulerTests
{
    [Fact]
    public async Task EnqueueAsync_ComposesKeyPrioritySingleFlightAndDispatcher()
    {
        SingleFlightJobFactory singleFlight = new();
        RecordingDispatcher dispatcher = new();
        SyncJobScheduler scheduler = new(singleFlight, dispatcher);
        PersistOhlcJobKey key = new(" btc ");
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int runs = 0;

        Task first = scheduler.EnqueueAsync(key, async _ =>
        {
            runs++;
            await release.Task;
        });
        Task duplicate = scheduler.EnqueueAsync(new PersistOhlcJobKey("BTC"), _ =>
        {
            runs++;
            return Task.CompletedTask;
        });
        duplicate.Should().BeSameAs(first);
        release.SetResult();
        await first;

        runs.Should().Be(1);
        dispatcher.LastPriority.Should().Be(SyncPriority.Interactive);
    }

    [Fact]
    public async Task DrainAsync_DelegatesToDispatcher()
    {
        RecordingDispatcher dispatcher = new();
        SyncJobScheduler scheduler = new(new SingleFlightJobFactory(), dispatcher);
        using CancellationTokenSource cancellation = new();

        await scheduler.DrainAsync(cancellation.Token);

        dispatcher.DrainToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public void JobKeys_DerivePriorityAndNormalizedIdentity()
    {
        PersistOhlcJobKey lower = new("btc");
        PersistOhlcJobKey upper = new(" BTC ");

        lower.Should().Be(upper);
        lower.Priority.Should().Be(SyncPriority.Interactive);
        new RefreshRatesJobKey().Priority.Should().Be(SyncPriority.Background);
    }

    [Fact]
    public async Task EnqueueAsync_AfterSuccessfulCompletion_StartsFreshRunForSameKey()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        SyncJobScheduler scheduler = new(new SingleFlightJobFactory(), dispatcher);
        PersistOhlcJobKey key = new("BTC");
        int runs = 0;

        Task first = scheduler.EnqueueAsync(key, _ =>
        {
            Interlocked.Increment(ref runs);
            return Task.CompletedTask;
        });
        await first;

        Task second = scheduler.EnqueueAsync(key, _ =>
        {
            Interlocked.Increment(ref runs);
            return Task.CompletedTask;
        });
        second.Should().NotBeSameAs(first);
        await second;

        runs.Should().Be(2);
    }

    [Fact]
    public async Task EnqueueAsync_AfterFault_PropagatesFailureThenAllowsRetry()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        SyncJobScheduler scheduler = new(new SingleFlightJobFactory(), dispatcher);
        RefreshRatesJobKey key = new();

        Task failed = scheduler.EnqueueAsync(
            key,
            _ => throw new InvalidOperationException("sync failed"));
        Func<Task> observeFailure = () => failed;
        await observeFailure.Should().ThrowAsync<InvalidOperationException>();

        Task retry = scheduler.EnqueueAsync(key, _ => Task.CompletedTask);
        retry.Should().NotBeSameAs(failed);
        await retry;
    }

    [Fact]
    public async Task EnqueueAsync_ConcurrentDuplicateKey_ReturnsSameTaskAndRunsOnce()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        SyncJobScheduler scheduler = new(new SingleFlightJobFactory(), dispatcher);
        PersistOhlcJobKey key = new("ETH");
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int runs = 0;
        Task? shared = null;
        Exception? fault = null;
        ConcurrentBag<Task> enrollments = new();
        using Barrier barrier = new(3);
        using CountdownEvent registered = new(3);

        for (int i = 0; i < 3; i++)
        {
            new Thread(_ =>
            {
                try
                {
                    barrier.SignalAndWait();
                    Task enqueued = scheduler.EnqueueAsync(key, async ct =>
                    {
                        Interlocked.Increment(ref runs);
                        await release.Task.WaitAsync(ct);
                    });
                    enrollments.Add(enqueued);
                    Interlocked.CompareExchange(ref shared, enqueued, null);
                }
                catch (Exception exception)
                {
                    fault = exception;
                }
                finally
                {
                    registered.Signal();
                }
            }).Start();
        }

        registered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        fault.Should().BeNull();
        shared.Should().NotBeNull();
        enrollments.Should().HaveCount(3);
        enrollments.Should().OnlyContain(task => ReferenceEquals(task, shared));

        release.SetResult();
        await shared!;

        Volatile.Read(ref runs).Should().Be(1);
    }

    [Fact]
    public async Task EnqueueAsync_ConcurrentDuplicateKey_EnqueuesDispatcherWorkOnce()
    {
        BlockingRecordingDispatcher dispatcher = new();
        SyncJobScheduler scheduler = new(new SingleFlightJobFactory(), dispatcher);
        PersistOhlcJobKey key = new("SOL");
        int runs = 0;
        Task? shared = null;
        Exception? fault = null;
        ConcurrentBag<Task> enrollments = new();
        using Barrier barrier = new(2);
        using CountdownEvent registered = new(2);

        for (int i = 0; i < 2; i++)
        {
            new Thread(_ =>
            {
                try
                {
                    barrier.SignalAndWait();
                    Task enqueued = scheduler.EnqueueAsync(key, async ct =>
                    {
                        Interlocked.Increment(ref runs);
                        await dispatcher.ReleaseTask.WaitAsync(ct);
                    });
                    enrollments.Add(enqueued);
                    Interlocked.CompareExchange(ref shared, enqueued, null);
                }
                catch (Exception exception)
                {
                    fault = exception;
                }
                finally
                {
                    registered.Signal();
                }
            }).Start();
        }

        registered.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        fault.Should().BeNull();
        shared.Should().NotBeNull();
        enrollments.Should().HaveCount(2);
        enrollments.Should().OnlyContain(task => ReferenceEquals(task, shared));
        dispatcher.EnqueueCount.Should().Be(1);

        dispatcher.Release();
        await shared!;

        Volatile.Read(ref runs).Should().Be(1);
    }

    private static PrioritizedJobDispatcher CreateDispatcher() =>
        new(Options.Create(new SyncSchedulerOptions { MaxConcurrency = 1 }));

    private sealed class RecordingDispatcher : IPrioritizedJobDispatcher
    {
        internal SyncPriority LastPriority { get; private set; }

        internal CancellationToken DrainToken { get; private set; }

        public Task EnqueueAsync(
            SyncPriority priority,
            Func<CancellationToken, Task> work,
            CancellationToken cancellationToken)
        {
            LastPriority = priority;
            return work(cancellationToken);
        }

        public Task DrainAsync(CancellationToken cancellationToken)
        {
            DrainToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingRecordingDispatcher : IPrioritizedJobDispatcher
    {
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _enqueueCount;

        internal int EnqueueCount => Volatile.Read(ref _enqueueCount);

        internal Task ReleaseTask => _release.Task;

        public Task EnqueueAsync(
            SyncPriority priority,
            Func<CancellationToken, Task> work,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _enqueueCount);
            return RunAsync(work, cancellationToken);
        }

        public Task DrainAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        internal void Release() => _release.SetResult();

        private async Task RunAsync(
            Func<CancellationToken, Task> work,
            CancellationToken cancellationToken)
        {
            await _release.Task.WaitAsync(cancellationToken);
            await work(cancellationToken);
        }
    }
}
