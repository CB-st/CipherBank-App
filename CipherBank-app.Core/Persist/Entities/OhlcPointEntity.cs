// <copyright file="OhlcPointEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

/// <summary>
/// One timestamped market value for a symbol (OHLC = open/high/low/close).
/// This row is a value series point, not a four-field candle. <see cref="Symbol"/>
/// stays a string; listed assets are not a closed enum.
/// </summary>
public sealed record OhlcPointEntity
{
    /// <summary>Listed asset ticker. String, not an enum — the set is not closed.</summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>Unix timestamp for this value-series point.</summary>
    public long Timestamp { get; set; }

    /// <summary>Quoted value at the timestamp (not open/high/low/close fields).</summary>
    public double Value { get; set; }
}
