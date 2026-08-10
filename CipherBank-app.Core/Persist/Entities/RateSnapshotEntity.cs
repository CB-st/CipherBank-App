// <copyright file="RateSnapshotEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed class RateSnapshotEntity
{
    public string Symbol { get; set; } = string.Empty;

    public double Usd { get; set; }

    public double Change24H { get; set; }

    public long UpdatedAtMs { get; set; }
}
