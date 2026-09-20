// <copyright file="ISyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Named, deduplicating task factory for market persist work (Interactive chart persist /
/// Background cold bootstrap).
/// Jobs dispatch through an injected <see cref="TaskScheduler"/> via <see cref="TaskFactory"/>;
/// waiting work is ordered Interactive-before-Background (FIFO within a lane) and the whole async job — not just
/// its first synchronous segment — counts against the mobile concurrency cap. The whole-job cap
/// and the keyed skip-duplicate contract are factory policy: a <see cref="TaskScheduler"/>
/// subclass caps only synchronous task segments (an async job frees its scheduler slot at the
/// first await), so inheritance cannot express either guarantee.
/// </summary>
public interface ISyncJobScheduler
{
    /// <summary>
    /// Enqueues keyed work and returns its observable completion. A duplicate key receives the
    /// existing job task; its cancellation token does not replace the accepted job's token.
    /// Use: High (Home market refresh). Scope: process-wide sync scheduler.
    /// </summary>
    Task EnqueueAsync(
        string key,
        SyncPriority priority,
        Func<CancellationToken, Task> work)
        => EnqueueAsync(key, priority, work, CancellationToken.None);

    Task EnqueueAsync(
        string key,
        SyncPriority priority,
        Func<CancellationToken, Task> work,
        CancellationToken ct);

    /// <summary>
    /// Waits for all queued and running work, propagating job faults and caller cancellation.
    /// Use: Medium (bounded shutdown and tests). Scope: process-wide sync scheduler.
    /// </summary>
    Task DrainAsync() => DrainAsync(CancellationToken.None);

    Task DrainAsync(CancellationToken ct);
}
