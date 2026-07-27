// <copyright file="StreamEvent.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;

namespace CipherBank_app.V1;

/// <summary>Stream event from /v1/stream.</summary>
public sealed class StreamEvent
{
    public string Type { get; init; } = string.Empty;

    public JsonElement? Payload { get; init; }
}
