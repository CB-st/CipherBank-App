// <copyright file="RefreshRatesJobKey.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>Identifies the process-wide background market-rate refresh.</summary>
public sealed record RefreshRatesJobKey() : SyncJobKey(SyncJobKind.RefreshRates);
