// <copyright file="ISyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Named, deduplicating, priority job queue for market persist work (P1 chart / P2 cold bootstrap).
/// Execution is delegated to an injected <see cref="TaskScheduler"/>; this type does not inherit
/// <see cref="TaskScheduler"/> or use <see cref="ThreadPriority"/> because those APIs do not provide
/// keyed skip-duplicates, P1-before-P2 among waiting work, a mobile concurrency cap, or <see cref="DrainAsync"/>.
/// </summary>
public interface ISyncJobScheduler
{
    /// <summary>
    /// Enqueues keyed work; duplicate keys already pending or in-flight are ignored.
    /// Use: High (Home market refresh). Scope: process-wide sync scheduler.
    /// </summary>
    void Enqueue(string key, SyncPriority priority, Func<CancellationToken, Task> work);

    /// <summary>
    /// Waits until the scheduler has no running or queued jobs.
    /// Use: Medium (tests / shutdown). Scope: process-wide sync scheduler.
    /// </summary>
    Task DrainAsync(CancellationToken ct);
}
