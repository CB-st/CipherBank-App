// <copyright file="PrioritizedJobDispatcher.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.Channels;
using CipherBank_app.Configuration;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Persist;

/// <inheritdoc cref="IPrioritizedJobDispatcher" />
public sealed class PrioritizedJobDispatcher : IPrioritizedJobDispatcher, IDisposable
{
    private static readonly IComparer<QueuedWork> _workComparer = QueuedWork.Comparer;

    private readonly Lock _gate = new();
    private readonly Channel<QueuedWork> _channel;
    private CancellationTokenSource? _shutdown = new();
    private HashSet<QueuedWork>? _jobs = [];
    private long _sequence;

    /// <summary>Initializes a new instance of the <see cref="PrioritizedJobDispatcher"/> class.</summary>
    /// <param name="options">Validated queue concurrency options.</param>
    public PrioritizedJobDispatcher(IOptions<SyncSchedulerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        int maxConcurrency = options.Value.Resolve();
        _channel = Channel.CreateUnboundedPrioritized(
            new UnboundedPrioritizedChannelOptions<QueuedWork>
            {
                Comparer = _workComparer,
                SingleReader = maxConcurrency == 1,
            });

        for (int i = 0; i < maxConcurrency; i++)
        {
            _ = ProcessQueueAsync();
        }
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        SyncPriority priority,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_jobs is null || _shutdown is null, this);
            CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _shutdown.Token);
            QueuedWork job = new(
                priority,
                ++_sequence,
                work,
                linkedCancellation);

            _jobs.Add(job);

            if (_channel.Writer.TryWrite(job))
            {
                return job.Task;
            }

            _jobs.Remove(job);
            linkedCancellation.Dispose();
            throw new ObjectDisposedException(nameof(PrioritizedJobDispatcher));
        }
    }

    /// <inheritdoc />
    public async Task DrainAsync(CancellationToken cancellationToken)
    {
        Task[] jobs;
        do
        {
            lock (_gate)
            {
                jobs = [.. _jobs?.Select(static job => job.Task) ?? []];
            }

            await Task.WhenAll(jobs.Select(static job => AwaitQuietlyAsync(job)))
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        while (jobs.Length > 0);
    }

    /// <summary>Cancels accepted work and completes the queue.</summary>
    public void Dispose()
    {
        List<QueuedWork> abandoned;
        CancellationTokenSource? shutdown;
        lock (_gate)
        {
            abandoned = [.. _jobs?.Where(static job => !job.Started) ?? []];
            _jobs = null;
            _channel.Writer.TryComplete();
            shutdown = _shutdown;
            _shutdown = null;
        }

        shutdown?.Cancel();
        foreach (QueuedWork job in abandoned)
        {
            job.Cancel();
        }

        shutdown?.Dispose();
    }

    private static async Task AwaitQuietlyAsync(Task job)
    {
        try
        {
            await job.ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (QueuedWork job in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            await ExecuteAsync(job).ConfigureAwait(false);
        }
    }

    private async Task ExecuteAsync(QueuedWork job)
    {
        bool run;
        lock (_gate)
        {
            run = _jobs is not null && job.TryBeginExecution();
        }

        try
        {
            if (run)
            {
                await job.RunAsync().ConfigureAwait(false);
            }
            else
            {
                job.Cancel();
            }
        }
        finally
        {
            lock (_gate)
            {
                _jobs?.Remove(job);
            }
        }
    }

    private sealed class QueuedWork
    {
        internal static readonly IComparer<QueuedWork> Comparer = Comparer<QueuedWork>.Create(
            static (left, right) =>
            {
                int priority = left._priority.CompareTo(right._priority);
                return priority != 0 ? priority : left._sequence.CompareTo(right._sequence);
            });

        private readonly SyncPriority _priority;
        private readonly long _sequence;
        private readonly Func<CancellationToken, Task> _work;
        private readonly CancellationTokenSource _cancellation;
        private readonly TaskCompletionSource _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal QueuedWork(
            SyncPriority priority,
            long sequence,
            Func<CancellationToken, Task> work,
            CancellationTokenSource cancellation)
        {
            _priority = priority;
            _sequence = sequence;
            _work = work;
            _cancellation = cancellation;
        }

        internal Task Task => _completion.Task;

        internal bool Started { get; private set; }

        internal bool TryBeginExecution()
        {
            Started = !_cancellation.IsCancellationRequested;
            return Started;
        }

        internal async Task RunAsync()
        {
            try
            {
                await _work(_cancellation.Token).ConfigureAwait(false);
                Complete();
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
            {
                Complete(canceled: true);
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException
                    || !_cancellation.IsCancellationRequested)
            {
                Complete(failure: exception);
            }
        }

        internal void Cancel() => Complete(canceled: true);

        private void Complete(Exception? failure = null, bool canceled = false)
        {
            bool completed;

            if (canceled)
            {
                completed = _completion.TrySetCanceled(_cancellation.Token);
            }
            else if (failure is not null)
            {
                completed = _completion.TrySetException(failure);
            }
            else
            {
                completed = _completion.TrySetResult();
            }

            if (completed)
            {
                _cancellation.Dispose();
            }
        }
    }
}
