// <copyright file="PersistOhlcJobKey.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>Identifies interactive chart persistence for one asset.</summary>
/// <param name="Symbol">Asset whose chart history is being persisted.</param>
public sealed record PersistOhlcJobKey(AssetSymbol Symbol) : SyncJobKey(SyncJobKind.PersistOhlc);
