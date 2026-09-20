// <copyright file="UserDataPackWire.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace CipherBank_app.UserData;

/// <summary>Cleartext pack envelope; only block ciphertext fields are secret.</summary>
public sealed class UserDataPackWire
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = UserDataConstants.PackFormat;

    [JsonPropertyName("content_version")]
    public uint ContentVersion { get; set; }

    [JsonPropertyName("username_hash_prefix")]
    public string UsernameHashPrefix { get; set; } = string.Empty;

    [JsonPropertyName("blocks")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<UserDataBlockWire> Blocks { get; } = [];
}
