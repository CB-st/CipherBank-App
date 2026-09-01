// <copyright file="SyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Configuration;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class SyncJobScheduler : ISyncJobScheduler
{
    private readonly object _gate = new();
    private readonly PriorityQueue<QueuedJob, (int Priority, long Sequence)> _queue = new();
    private readonly HashSet<string> _queuedKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> _inFlightKeys = new(StringComparer.Ordinal);
    private readonly TaskScheduler _taskScheduler;
    private readonly int _maxConcurrency;
    private long _sequence;
    private int _running;
    private TaskCompletionSource? _idleSignal;

    public SyncJobScheduler()
        : this(TaskScheduler.Default, new SyncSchedulerOptions())
    {
    }

    public SyncJobScheduler(TaskScheduler taskScheduler, SyncSchedulerOptions options)
    {
        _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        ArgumentNullException.ThrowIfNull(options);
        _maxConcurrency = options.Resolve();
    }

    /// <inheritdoc />
    public void Enqueue(string key, SyncPriority priority, Func<CancellationToken, Task> work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(work);

        lock (_gate)
        {
            if (_inFlightKeys.Contains(key) || _queuedKeys.Contains(key))
            {
                return;
            }

            long sequence = ++_sequence;
            _queue.Enqueue(new QueuedJob(key, work), ((int)priority, sequence));
            _queuedKeys.Add(key);
        }

        Pump();
    }

    /// <inheritdoc />
    public async Task DrainAsync(CancellationToken ct)
    {
        while (true)
        {
            Task waitTask;
            lock (_gate)
            {
                if (_running == 0 && _queue.Count == 0)
                {
                    return;
                }

                _idleSignal ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                waitTask = _idleSignal.Task;
            }

            await waitTask.WaitAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Starts as many eligible jobs as concurrency allows.
    /// Use: High (after each enqueue / completion). Scope: SyncJobScheduler instance.
    /// </summary>
    private void Pump()
    {
        while (true)
        {
            QueuedJob job;
            lock (_gate)
            {
                if (_running >= _maxConcurrency || _queue.Count == 0)
                {
                    return;
                }

                job = _queue.Dequeue();
                _queuedKeys.Remove(job.Key);
                _running++;
                _inFlightKeys.Add(job.Key);
            }

            _ = Task.Factory.StartNew(
                    static state =>
                    {
                        DispatchState dispatch = (DispatchState)state!;
                        return dispatch.Owner.RunJobAsync(dispatch.Job);
                    },
                    new DispatchState(this, job),
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    _taskScheduler)
                .Unwrap();
        }
    }

    /// <summary>
    /// Runs one job then pumps the next eligible work.
    /// Use: High (per enqueued job). Scope: SyncJobScheduler instance.
    /// </summary>
    private async Task RunJobAsync(QueuedJob job)
    {
        try
        {
            await job.Work(CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Jobs own their errors.
        }
        catch (ObjectDisposedException)
        {
            // Jobs own their errors.
        }
        catch (InvalidOperationException)
        {
            // Jobs own their errors.
        }
        catch (IOException)
        {
            // Jobs own their errors.
        }
        finally
        {
            lock (_gate)
            {
                _running--;
                _inFlightKeys.Remove(job.Key);
                if (_running == 0 && _queue.Count == 0)
                {
                    _idleSignal?.TrySetResult();
                    _idleSignal = null;
                }
            }

            Pump();
        }
    }

    private sealed record QueuedJob(
        string Key,
        Func<CancellationToken, Task> Work);

    private sealed record DispatchState(SyncJobScheduler Owner, QueuedJob Job);
}
