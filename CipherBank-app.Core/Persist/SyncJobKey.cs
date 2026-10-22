// <copyright file="SyncJobKey.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Immutable value identity for one class of synchronization work.
/// </summary>
public abstract record SyncJobKey(SyncJobKind Kind)
{
    /// <summary>Gets the queue priority defined by this job kind.</summary>
    public SyncPriority Priority => Kind switch
    {
        SyncJobKind.PersistOhlc => SyncPriority.Interactive,
        SyncJobKind.RefreshRates => SyncPriority.Background,
        _ => throw new InvalidOperationException($"Unsupported sync job kind: {Kind}."),
    };
}
