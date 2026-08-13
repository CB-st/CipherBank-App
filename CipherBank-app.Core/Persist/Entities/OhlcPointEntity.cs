// <copyright file="OhlcPointEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record OhlcPointEntity
{
    public string Symbol { get; set; } = string.Empty;

    public long Timestamp { get; set; }

    public double Value { get; set; }
}
