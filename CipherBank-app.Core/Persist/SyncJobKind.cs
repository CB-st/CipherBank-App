// <copyright file="SyncJobKind.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Closed set of application-controlled synchronization job kinds.</summary>
public enum SyncJobKind
{
    /// <summary>Persists interactive chart history for one asset.</summary>
    PersistOhlc,

    /// <summary>Refreshes the shared background market-rate snapshot.</summary>
    RefreshRates,
}
