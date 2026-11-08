// <copyright file="RateSnapshotEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record RateSnapshotEntity
{
    public string Symbol { get; init; } = string.Empty;

    public decimal Usd { get; init; }

    public decimal Change24H { get; init; }

    public long UpdatedAtMs { get; init; }
}
