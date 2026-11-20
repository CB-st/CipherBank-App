// <copyright file="HistoryPointDto.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>OHLC / history point.</summary>
public sealed class HistoryPointDto
{
    [JsonPropertyName("T")]
    public long T { get; set; }

    [JsonPropertyName("V")]
    public double V { get; set; }
}
