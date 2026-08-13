// <copyright file="UserDataBlockWire.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.UserData;

/// <summary>One sealed block inside a userdata pack envelope.</summary>
public sealed class UserDataBlockWire
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("seq")]
    public uint Seq { get; set; }

    [JsonPropertyName("alg")]
    public string Algorithm { get; set; } = UserDataConstants.BlockAlgorithm;

    [JsonPropertyName("nonce_b64")]
    public string NonceBase64 { get; set; } = string.Empty;

    [JsonPropertyName("tag_b64")]
    public string TagBase64 { get; set; } = string.Empty;

    [JsonPropertyName("ciphertext_b64")]
    public string CiphertextBase64 { get; set; } = string.Empty;
}
