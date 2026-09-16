// <copyright file="SingleFlightJobFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Concurrent;

namespace CipherBank_app.Persist;

/// <inheritdoc cref="ISingleFlightJobFactory" />
public sealed class SingleFlightJobFactory : ISingleFlightJobFactory
{
    private readonly ConcurrentDictionary<SyncJobKey, Lazy<Task>> _jobs = new();

    /// <inheritdoc />
    public Task GetOrCreateAsync(SyncJobKey key, Func<Task> create)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(create);

        Lazy<Task> candidate = null!;
        candidate = new Lazy<Task>(
            () => RunAndReleaseAsync(key, candidate, create),
            LazyThreadSafetyMode.ExecutionAndPublication);
        return _jobs.GetOrAdd(key, candidate).Value;
    }

    private async Task RunAndReleaseAsync(
        SyncJobKey key,
        Lazy<Task> entry,
        Func<Task> create)
    {
        try
        {
            await create().ConfigureAwait(false);
        }
        finally
        {
            _jobs.TryRemove(new KeyValuePair<SyncJobKey, Lazy<Task>>(key, entry));
        }
    }
}
