// <copyright file="QuoteDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Quote lock response.</summary>
public sealed class QuoteDto
{
    [JsonPropertyName("FROM")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("TO")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("RATE")]
    public string Rate { get; set; } = "0";

    [JsonPropertyName("EXPIRES_AT")]
    public long ExpiresAt { get; set; }
}
