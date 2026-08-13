// <copyright file="PosSessionDto.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>POS session.</summary>
public sealed class PosSessionDto
{
    [JsonPropertyName("SESSION_ID")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("STATUS")]
    public string Status { get; set; } = "pending_auth";

    [JsonPropertyName("TOKEN_REF")]
    public string? TokenRef { get; set; }

    [JsonPropertyName("LAST4")]
    public string? Last4 { get; set; }

    [JsonPropertyName("BRAND")]
    public string? Brand { get; set; }

    [JsonPropertyName("TTL_MS")]
    public long? TtlMs { get; set; }
}
