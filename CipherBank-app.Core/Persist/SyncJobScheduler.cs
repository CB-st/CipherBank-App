// <copyright file="SyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <inheritdoc cref="ISyncJobScheduler" />
public sealed class SyncJobScheduler : ISyncJobScheduler
{
    private readonly ISingleFlightJobFactory _singleFlight;
    private readonly IPrioritizedJobDispatcher _queue;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncJobScheduler"/> class.
    /// </summary>
    /// <param name="singleFlight">Keyed in-flight operation policy.</param>
    /// <param name="queue">Prioritized whole-operation queue.</param>
    public SyncJobScheduler(
        ISingleFlightJobFactory singleFlight,
        IPrioritizedJobDispatcher queue)
    {
        ArgumentNullException.ThrowIfNull(singleFlight);
        ArgumentNullException.ThrowIfNull(queue);
        _singleFlight = singleFlight;
        _queue = queue;
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        SyncJobKey key,
        Func<CancellationToken, Task> work)
        => EnqueueAsync(key, work, CancellationToken.None);

    /// <inheritdoc />
    public Task EnqueueAsync(
        SyncJobKey key,
        Func<CancellationToken, Task> work,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(work);
        return _singleFlight.GetOrCreateAsync(
            key,
            () => _queue.EnqueueAsync(key.Priority, work, ct));
    }

    /// <inheritdoc />
    public Task DrainAsync() => DrainAsync(CancellationToken.None);

    /// <inheritdoc />
    public Task DrainAsync(CancellationToken ct) => _queue.DrainAsync(ct);
}
