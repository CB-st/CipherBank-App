// <copyright file="RateSnapshotEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record RateSnapshotEntity
{
    public string Symbol { get; set; } = string.Empty;

    public double Usd { get; set; }

    public double Change24H { get; set; }

    public long UpdatedAtMs { get; set; }
}
