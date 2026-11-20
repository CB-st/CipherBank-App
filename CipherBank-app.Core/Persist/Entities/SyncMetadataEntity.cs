// <copyright file="SyncMetadataEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record SyncMetadataEntity
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public long UpdatedAtMs { get; init; }
}
