// <copyright file="MoneyMoveDto.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Money movement ack.</summary>
public sealed class MoneyMoveDto
{
    [JsonPropertyName("ID")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("STATUS")]
    public string Status { get; set; } = "pending";
}
