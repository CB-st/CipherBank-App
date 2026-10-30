// <copyright file="PersistOhlcJobKey.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>Identifies interactive chart persistence for one asset.</summary>
public sealed record PersistOhlcJobKey : SyncJobKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistOhlcJobKey"/> class.
    /// </summary>
    /// <param name="symbol">Asset whose chart history is being persisted.</param>
    public PersistOhlcJobKey(AssetSymbol symbol)
        : base(SyncJobKind.PersistOhlc)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        Symbol = symbol;
    }

    /// <summary>Gets the scoped asset.</summary>
    public AssetSymbol Symbol { get; }
}
