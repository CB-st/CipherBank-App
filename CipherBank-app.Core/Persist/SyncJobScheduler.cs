// <copyright file="SyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.Channels;
using CipherBank_app.Configuration;

namespace CipherBank_app.Persist;

/// <inheritdoc cref="ISyncJobScheduler" />
public sealed class SyncJobScheduler : ISyncJobScheduler, IDisposable
{
    private static readonly IComparer<QueuedJob> _jobComparer = Comparer<QueuedJob>.Create(
        static (left, right) =>
        {
            int priority = left.Priority.CompareTo(right.Priority);
            return priority != 0 ? priority : left.Sequence.CompareTo(right.Sequence);
        });

    private readonly Lock _gate = new();
    private readonly Channel<QueuedJob> _channel;
    private readonly Dictionary<string, QueuedJob> _jobs = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _shutdown = new();
    private long _sequence;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncJobScheduler"/> class with default options.
    /// Use: Medium (tests). Scope: persist.
    /// </summary>
    public SyncJobScheduler()
        : this(new SyncSchedulerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncJobScheduler"/> class.
    /// Use: Medium (host DI / tests). Scope: persist.
    /// </summary>
    public SyncJobScheduler(SyncSchedulerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        int maxConcurrency = options.Resolve();
        _channel = Channel.CreateUnboundedPrioritized(
            new UnboundedPrioritizedChannelOptions<QueuedJob>
            {
                AllowSynchronousContinuations = false,
                Comparer = _jobComparer,
                SingleReader = maxConcurrency == 1,
                SingleWriter = false,
            });

        for (int i = 0; i < maxConcurrency; i++)
        {
            _ = Task.Run(ProcessQueueAsync);
        }
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        string key,
        SyncPriority priority,
        Func<CancellationToken, Task> work)
        => EnqueueAsync(key, priority, work, CancellationToken.None);

    /// <inheritdoc />
    public Task EnqueueAsync(
        string key,
        SyncPriority priority,
        Func<CancellationToken, Task> work,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(work);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_jobs.TryGetValue(key, out QueuedJob? existing))
            {
                return existing.Completion.Task;
            }

            CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                ct,
                _shutdown.Token);
            QueuedJob job = new(
                key,
                priority,
                ++_sequence,
                work,
                cancellation);
            _jobs.Add(key, job);
            if (!_channel.Writer.TryWrite(job))
            {
                _jobs.Remove(key);
                cancellation.Dispose();
                throw new ObjectDisposedException(nameof(SyncJobScheduler));
            }

            return job.Completion.Task;
        }
    }

    /// <inheritdoc />
    public Task DrainAsync() => DrainAsync(CancellationToken.None);

    /// <inheritdoc />
    public async Task DrainAsync(CancellationToken ct)
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

                jobs = _jobs.Values.Select(job => job.Completion.Task).ToArray();
            }

            await Task.WhenAll(jobs).WaitAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Cancels running jobs and completes queued-but-unstarted jobs as canceled during
    /// container shutdown. No queued delegate starts after disposal; running jobs finish
    /// under their canceled linked token.
    /// Use: Low (container shutdown). Scope: process-wide scheduler.
    /// </summary>
    public void Dispose()
    {
        List<QueuedJob> abandoned = new();
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _channel.Writer.TryComplete();
            abandoned.AddRange(_jobs.Values.Where(static job => !job.Started));
            foreach (QueuedJob job in abandoned)
            {
                _jobs.Remove(job.Key);
            }

            while (_channel.Reader.TryRead(out _))
            {
            }
        }

        _shutdown.Cancel();
        foreach (QueuedJob job in abandoned)
        {
            job.Completion.TrySetCanceled(job.Cancellation.Token);
            job.Cancellation.Dispose();
        }

        _shutdown.Dispose();
    }

    /// <summary>
    /// Consumes prioritized jobs sequentially within one logical worker.
    /// Use: High (one loop per configured worker). Scope: SyncJobScheduler instance.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        await foreach (QueuedJob job in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            await ExecuteJobAsync(job).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes one complete asynchronous job and transfers its terminal state to callers.
    /// Use: High (per dequeued job). Scope: channel worker.
    /// </summary>
    private async Task ExecuteJobAsync(QueuedJob job)
    {
        bool schedulerDisposed;
        bool shouldRun;
        lock (_gate)
        {
            schedulerDisposed = _disposed;
            shouldRun = !schedulerDisposed && !job.Cancellation.IsCancellationRequested;
            if (shouldRun)
            {
                job.Started = true;
            }
            else if (!schedulerDisposed)
            {
                _jobs.Remove(job.Key);
            }
        }

        if (!shouldRun)
        {
            if (schedulerDisposed)
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
        {
            failure = exception;
        }

        lock (_gate)
        {
            _jobs.Remove(job.Key);
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

    private sealed record QueuedJob(
        string Key,
        SyncPriority Priority,
        long Sequence,
        Func<CancellationToken, Task> Work,
        CancellationTokenSource Cancellation)
    {
        internal bool Started { get; set; }

        internal TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
