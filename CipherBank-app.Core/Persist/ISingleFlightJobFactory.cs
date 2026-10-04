// <copyright file="ISingleFlightJobFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Coalesces concurrent requests for the same synchronization job key.</summary>
public interface ISingleFlightJobFactory
{
    /// <summary>Returns the active task for <paramref name="key"/>, creating it once when absent.</summary>
    /// <param name="key">Semantic identity of the operation.</param>
    /// <param name="create">Creates the operation only for the winning caller.</param>
    /// <returns>The shared operation task.</returns>
    Task GetOrCreateAsync(SyncJobKey key, Func<Task> create);
}
