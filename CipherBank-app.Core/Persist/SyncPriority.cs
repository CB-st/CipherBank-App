// <copyright file="SyncPriority.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Queue-ordering lane for sync jobs: the value is the <c>PriorityQueue</c> key, and the
/// lower value dequeues first (FIFO within a lane). This is deliberately not
/// <see cref="ThreadPriority"/> — that enum's contract is OS thread scheduling, its values
/// ascend with urgency (inverted relative to this queue), and no job here touches thread
/// priorities. Precedent: WPF's DispatcherPriority and Win32's TP_CALLBACK_PRIORITY also
/// define their own work-ordering vocabularies instead of reusing the thread enum.
/// </summary>
public enum SyncPriority
{
    /// <summary>Work the user is watching (for example chart persist). Dequeues first.</summary>
    Interactive = 1,

    /// <summary>Deferred hydration (for example cold-start rate bootstrap).</summary>
    Background = 2,
}
