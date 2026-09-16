// <copyright file="SingleFlightJobFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Concurrent;

namespace CipherBank_app.Persist;

/// <inheritdoc cref="ISingleFlightJobFactory" />
public sealed class SingleFlightJobFactory : ISingleFlightJobFactory
{
    private readonly ConcurrentDictionary<SyncJobKey, Entry> _jobs = new();

    /// <inheritdoc />
    public Task GetOrCreateAsync(SyncJobKey key, Func<Task> create)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(create);

        Entry candidate = new(this, key, create);
        return _jobs.GetOrAdd(key, candidate).Task;
    }

    private async Task RunAndReleaseAsync(
        SyncJobKey key,
        Entry entry,
        Func<Task> create)
    {
        try
        {
            await create().ConfigureAwait(false);
        }
        finally
        {
            _jobs.TryRemove(new KeyValuePair<SyncJobKey, Entry>(key, entry));
        }
    }

    private sealed class Entry
    {
        private readonly Lazy<Task> _task;

        internal Entry(
            SingleFlightJobFactory owner,
            SyncJobKey key,
            Func<Task> create)
        {
            _task = new Lazy<Task>(
                () => owner.RunAndReleaseAsync(key, this, create),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        internal Task Task => _task.Value;
    }
}
