// <copyright file="ISyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Named, deduplicating queue for market persist work (Interactive chart persist /
/// Background cold bootstrap). A fixed set of asynchronous channel consumers orders waiting
/// work Interactive-before-Background (FIFO within a lane) and counts each whole async job
/// against the mobile concurrency cap.
/// </summary>
public interface ISyncJobScheduler
{
    /// <summary>
    /// Enqueues keyed work and returns its observable completion. A duplicate key receives the
    /// existing job task; its cancellation token does not replace the accepted job's token.
    /// Use: High (Home market refresh). Scope: process-wide sync scheduler.
    /// </summary>
    Task EnqueueAsync(
        SyncJobKey key,
        Func<CancellationToken, Task> work)
        => EnqueueAsync(key, work, CancellationToken.None);

    Task EnqueueAsync(
        SyncJobKey key,
        Func<CancellationToken, Task> work,
        CancellationToken ct);

    /// <summary>
    /// Waits for all queued and running work, propagating job faults and caller cancellation.
    /// Use: Medium (bounded shutdown and tests). Scope: process-wide sync scheduler.
    /// </summary>
    Task DrainAsync() => DrainAsync(CancellationToken.None);

    Task DrainAsync(CancellationToken ct);
}
