// <copyright file="SyncMetadataEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record SyncMetadataEntity
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public long UpdatedAtMs { get; set; }
}
