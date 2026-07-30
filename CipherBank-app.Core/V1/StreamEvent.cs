// <copyright file="StreamEvent.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CipherBank_app.V1;

/// <summary>Stream event from /v1/stream.</summary>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Wire stream payload name matches API TYPE field; inherits EventArgs for EventHandler<T>.")]
public sealed class StreamEvent : EventArgs
{
    public string Type { get; init; } = string.Empty;

    public JsonElement? Payload { get; init; }
}
