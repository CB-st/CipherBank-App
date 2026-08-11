// <copyright file="SessionDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Session tokens.</summary>
public sealed class SessionDto
{
    [JsonPropertyName("ACCESS_TOKEN")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("REFRESH_TOKEN")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("EXPIRES_AT")]
    public long ExpiresAt { get; set; }
}
