// <copyright file="PreferenceEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

public sealed record PreferenceEntity
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
