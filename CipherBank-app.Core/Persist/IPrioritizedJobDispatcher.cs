// <copyright file="IPrioritizedJobDispatcher.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Dispatches prioritized work while bounding complete asynchronous operations.</summary>
public interface IPrioritizedJobDispatcher
{
    /// <summary>Queues work and returns its observable completion.</summary>
    /// <param name="priority">Application queue priority.</param>
    /// <param name="work">Complete asynchronous operation.</param>
    /// <param name="cancellationToken">Caller cancellation linked to queue shutdown.</param>
    /// <returns>The queued operation completion.</returns>
    Task EnqueueAsync(
        SyncPriority priority,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken);

    /// <summary>Waits until all accepted work reaches a terminal state.</summary>
    /// <param name="cancellationToken">Cancellation for the wait only.</param>
    /// <returns>A task representing the drain.</returns>
    Task DrainAsync(CancellationToken cancellationToken);
}
