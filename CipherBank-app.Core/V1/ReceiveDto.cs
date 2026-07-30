// <copyright file="ReceiveDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Receive address payload.</summary>
public sealed class ReceiveDto
{
    [JsonPropertyName("ASSET")]
    public string Asset { get; set; } = string.Empty;

    [JsonPropertyName("ADDRESS")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("URI")]
    public System.Uri? Uri { get; set; }
}
