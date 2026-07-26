// <copyright file="ISyncJobQueue.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Sync job priority — lower value runs first.</summary>
public enum SyncPriority
{
    P1 = 1,
    P2 = 2,
}

/// <summary>Priority job queue for market persist work (P1 chart / P2 cold bootstrap).</summary>
public interface ISyncJobQueue
{
    void Enqueue(string key, SyncPriority priority, Func<CancellationToken, Task> work);

    Task DrainAsync(CancellationToken ct);
}
