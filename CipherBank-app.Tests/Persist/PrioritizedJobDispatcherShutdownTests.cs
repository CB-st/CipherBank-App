// <copyright file="PrioritizedJobDispatcherShutdownTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Persist;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CipherBank_app.Tests.Persist;

/// <summary>
/// Dispatcher shutdown probes that watch <see cref="TaskScheduler.UnobservedTaskException"/>.
/// That event is process-wide, so these run in a collection with parallelization disabled.
/// </summary>
[Collection(UnobservedTaskExceptionIsolation.Name)]
public sealed class PrioritizedJobDispatcherShutdownTests
{
    [Fact]
    public async Task Dispose_DoesNotThrowWhenShutdownCancelsAnAlreadyCompletedJob()
    {
        using PrioritizedJobDispatcher dispatcher = CreateDispatcher();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int queuedRuns = 0;
        Task running = dispatcher.EnqueueAsync(
            SyncPriority.Interactive,
            async ct =>
            {
                started.SetResult();

                // Completing inline lets shutdown cancellation finish the consumer, including the
                // second Cancel of the queued job, before Dispose cancels that job again.
                TaskCompletionSource released = new();
                using CancellationTokenRegistration registration = ct.Register(
                    static state => ((TaskCompletionSource)state!).TrySetResult(),
                    released);
                await released.Task.ConfigureAwait(false);
            },
            CancellationToken.None);
        await started.Task;
        Task queued = dispatcher.EnqueueAsync(
            SyncPriority.Background,
            _ =>
            {
                Interlocked.Increment(ref queuedRuns);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        List<Exception> unobserved = [];

        // Record only. Marking exceptions observed would change the outcome for other tests' tasks.
        EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, args) =>
        {
            lock (unobserved)
            {
                unobserved.Add(args.Exception);
            }
        };

        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            Action dispose = dispatcher.Dispose;
            dispose.Should().NotThrow();
            await running.WaitAsync(TimeSpan.FromSeconds(5));
            for (int attempt = 0; attempt < 3; attempt++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
            }
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= handler;
        }

        queued.IsCanceled.Should().BeTrue();
        Volatile.Read(ref queuedRuns).Should().Be(0);
        unobserved.Exists(IsDispatcherObjectDisposed).Should().BeFalse();
    }

    private static bool IsDispatcherObjectDisposed(Exception exception) =>
        exception is AggregateException aggregate
        && aggregate.Flatten().InnerExceptions.Any(static inner =>
            inner is ObjectDisposedException
            && inner.StackTrace?.Contains(
                nameof(PrioritizedJobDispatcher),
                StringComparison.Ordinal) == true);

    private static PrioritizedJobDispatcher CreateDispatcher(int maxConcurrency = 1) =>
        new(Options.Create(new SyncSchedulerOptions { MaxConcurrency = maxConcurrency }));
}
