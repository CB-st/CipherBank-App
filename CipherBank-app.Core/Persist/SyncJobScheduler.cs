// <copyright file="SyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <inheritdoc cref="ISyncJobScheduler" />
public sealed class SyncJobScheduler(
    ISingleFlightJobFactory singleFlight,
    IPrioritizedJobDispatcher queue)
    : ISyncJobScheduler
{
    private readonly ISingleFlightJobFactory _singleFlight = singleFlight;
    private readonly IPrioritizedJobDispatcher _queue = queue;

    /// <inheritdoc />
    public Task EnqueueAsync(
        SyncJobKey key,
        Func<CancellationToken, Task> work)
        => EnqueueAsync(key, work, CancellationToken.None);

    /// <inheritdoc />
    public Task EnqueueAsync(
        SyncJobKey key,
        Func<CancellationToken, Task> work,
        CancellationToken ct) =>
        _singleFlight.GetOrCreateAsync(
            key,
            () => _queue.EnqueueAsync(key.Priority, work, ct));

    /// <inheritdoc />
    public Task DrainAsync() => DrainAsync(CancellationToken.None);

    /// <inheritdoc />
    public Task DrainAsync(CancellationToken ct) => _queue.DrainAsync(ct);
}
