// <copyright file="StreamEventArgs.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;

namespace CipherBank_app.V1;

/// <summary>Stream event from /v1/stream.</summary>
public sealed class StreamEventArgs : EventArgs
{
    public string Type { get; init; } = string.Empty;

    public JsonElement? Payload { get; init; }
}
