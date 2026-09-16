// <copyright file="PrioritizedJobDispatcher.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.Channels;
using CipherBank_app.Configuration;

namespace CipherBank_app.Persist;

/// <inheritdoc cref="IPrioritizedJobDispatcher" />
public sealed class PrioritizedJobDispatcher : IPrioritizedJobDispatcher, IDisposable
{
    private static readonly IComparer<QueuedWork> WorkComparer = Comparer<QueuedWork>.Create(
        static (left, right) =>
        {
            int priority = left.Priority.CompareTo(right.Priority);
            return priority != 0 ? priority : left.Sequence.CompareTo(right.Sequence);
        });

    private readonly Lock _gate = new();
    private readonly Channel<QueuedWork> _channel;
    private readonly HashSet<QueuedWork> _jobs = [];
    private readonly CancellationTokenSource _shutdown = new();
    private long _sequence;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="PrioritizedJobDispatcher"/> class.</summary>
    /// <param name="options">Validated queue concurrency options.</param>
    public PrioritizedJobDispatcher(SyncSchedulerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        int maxConcurrency = options.Resolve();
        _channel = Channel.CreateUnboundedPrioritized(
            new UnboundedPrioritizedChannelOptions<QueuedWork>
            {
                Comparer = WorkComparer,
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
            ObjectDisposedException.ThrowIf(_disposed, this);
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
            if (!_channel.Writer.TryWrite(job))
            {
                _jobs.Remove(job);
                linkedCancellation.Dispose();
                throw new ObjectDisposedException(nameof(PrioritizedJobDispatcher));
            }

            return job.Completion.Task;
        }
    }

    /// <inheritdoc />
    public async Task DrainAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task[] jobs;
            lock (_gate)
            {
                if (_jobs.Count == 0)
                {
                    return;
                }

                jobs = _jobs.Select(job => job.Completion.Task).ToArray();
            }

            await Task.WhenAll(jobs).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Cancels accepted work and completes the queue.</summary>
    public void Dispose()
    {
        List<QueuedWork> abandoned = [];
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _channel.Writer.TryComplete();
            abandoned.AddRange(_jobs.Where(static job => !job.Started));
            foreach (QueuedWork job in abandoned)
            {
                _jobs.Remove(job);
            }
        }

        _shutdown.Cancel();
        foreach (QueuedWork job in abandoned)
        {
            job.Completion.TrySetCanceled(job.Cancellation.Token);
            job.Cancellation.Dispose();
        }

        _shutdown.Dispose();
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
        bool queueDisposed;
        bool shouldRun;
        lock (_gate)
        {
            queueDisposed = _disposed;
            shouldRun = !queueDisposed && !job.Cancellation.IsCancellationRequested;
            if (shouldRun)
            {
                job.Started = true;
            }
            else if (!queueDisposed)
            {
                _jobs.Remove(job);
            }
            else
            {
                // Dispose owns completion and cancellation for dequeued work that never started.
            }
        }

        if (!shouldRun)
        {
            if (queueDisposed)
            {
                return;
            }

            job.Completion.TrySetCanceled(job.Cancellation.Token);
            job.Cancellation.Dispose();
            return;
        }

        Exception? failure = null;
        bool canceled = false;
        try
        {
            await job.Work(job.Cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (job.Cancellation.IsCancellationRequested)
        {
            canceled = true;
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException
                || !job.Cancellation.IsCancellationRequested)
        {
            failure = exception;
        }

        lock (_gate)
        {
            _jobs.Remove(job);
        }

        if (canceled)
        {
            job.Completion.TrySetCanceled(job.Cancellation.Token);
        }
        else if (failure is not null)
        {
            job.Completion.TrySetException(failure);
        }
        else
        {
            job.Completion.TrySetResult();
        }

        job.Cancellation.Dispose();
    }

    private sealed class QueuedWork
    {
        internal QueuedWork(
            SyncPriority priority,
            long sequence,
            Func<CancellationToken, Task> work,
            CancellationTokenSource cancellation)
        {
            Priority = priority;
            Sequence = sequence;
            Work = work;
            Cancellation = cancellation;
        }

        internal SyncPriority Priority { get; }

        internal long Sequence { get; }

        internal Func<CancellationToken, Task> Work { get; }

        internal CancellationTokenSource Cancellation { get; }

        internal bool Started { get; set; }

        internal TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
