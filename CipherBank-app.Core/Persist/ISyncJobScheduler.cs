// <copyright file="ISyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Priority job scheduler for market persist work (P1 chart / P2 cold bootstrap).</summary>
public interface ISyncJobScheduler
{
    void Enqueue(string key, SyncPriority priority, Func<CancellationToken, Task> work);

    Task DrainAsync(CancellationToken ct);
}
