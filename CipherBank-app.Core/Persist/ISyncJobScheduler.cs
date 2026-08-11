// <copyright file="ISyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Priority job scheduler for market persist work (P1 chart / P2 cold bootstrap).</summary>
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
