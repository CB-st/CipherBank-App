// <copyright file="SyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class SyncJobScheduler : ISyncJobScheduler
{
    private const int MaxConcurrency = 2;

    private readonly object _gate = new();
    private readonly List<QueuedJob> _queue = [];
    private readonly HashSet<string> _inFlightKeys = new(StringComparer.Ordinal);
    private int _running;
    private TaskCompletionSource? _idleSignal;

    /// <inheritdoc />
    public void Enqueue(string key, SyncPriority priority, Func<CancellationToken, Task> work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(work);

        lock (_gate)
        {
            if (_inFlightKeys.Contains(key) || _queue.Exists(job => job.Key == key))
            {
                return;
            }

            _queue.Add(new QueuedJob(key, priority, work, Environment.TickCount64));
            _queue.Sort(static (left, right) =>
            {
                int byPriority = left.Priority.CompareTo(right.Priority);
                return byPriority != 0 ? byPriority : left.EnqueuedAt.CompareTo(right.EnqueuedAt);
            });
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

    private void Pump()
    {
        while (true)
        {
            QueuedJob? job = null;
            lock (_gate)
            {
                if (_running >= MaxConcurrency || _queue.Count == 0)
                {
                    return;
                }

                int nextIndex = _queue.FindIndex(candidate =>
                    !_inFlightKeys.Contains(candidate.Key));
                if (nextIndex < 0)
                {
                    return;
                }

                job = _queue[nextIndex];
                _queue.RemoveAt(nextIndex);
                _running++;
                _inFlightKeys.Add(job.Key);
            }

            _ = RunJobAsync(job);
        }
    }

    private async Task RunJobAsync(QueuedJob job)
    {
        try
        {
            await job.Work(CancellationToken.None).ConfigureAwait(false);
        }
        catch
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
        SyncPriority Priority,
        Func<CancellationToken, Task> Work,
        long EnqueuedAt);
}
