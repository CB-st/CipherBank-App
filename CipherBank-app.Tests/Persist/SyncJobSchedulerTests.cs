// <copyright file="SyncJobSchedulerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
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
}
