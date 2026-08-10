// <copyright file="OhlcPointEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist.Entities;

internal sealed class OhlcPointEntity
{
    public string Symbol { get; set; } = string.Empty;

    public long Timestamp { get; set; }

    public double Value { get; set; }
}
