// <copyright file="PlainJsonUserDataWireCodec.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CipherBank_app.UserData;

/// <summary>
/// Loopback-compatible codec: outer JSON with nested JSON object PAYLOAD (no CB_MASTER_KEY).
/// Production src encrypts PAYLOAD; swap to a MasterKey codec when keys are available.
/// </summary>
public sealed class PlainJsonUserDataWireCodec : IUserDataWireCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly TimeProvider _time;

    public PlainJsonUserDataWireCodec()
        : this(TimeProvider.System)
    {
    }

    public PlainJsonUserDataWireCodec(TimeProvider timeProvider)
    {
        _time = timeProvider ?? TimeProvider.System;
    }

    public UserDataPayloadMode Mode => UserDataPayloadMode.PlainJson;

    /// <inheritdoc />
    public string Encode(string messageType, long code, string message, IReadOnlyDictionary<string, string> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(payload);

        var payloadNode = new JsonObject();
        foreach (KeyValuePair<string, string> kv in payload)
        {
            if (kv.Key.StartsWith("__", StringComparison.Ordinal))
            {
                continue;
            }

            payloadNode[kv.Key] = kv.Value;
        }

        long ts = _time.GetUtcNow().ToUnixTimeSeconds();
        var root = new JsonObject
        {
            [UserDataWireNames.MessageType] = messageType,
            [UserDataWireNames.TimeStamp] = ts,
            [UserDataWireNames.Code] = code,
            [UserDataWireNames.Message] = message ?? string.Empty,
            [UserDataWireNames.Payload] = payloadNode,
        };

        return root.ToJsonString(JsonOptions);
    }

    /// <inheritdoc />
    public UserDataApiFrame Decode(string frameText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(frameText);
        JsonNode root = JsonNode.Parse(frameText)
            ?? throw new InvalidOperationException("Empty userdata frame JSON.");

        string messageType = root[UserDataWireNames.MessageType]?.GetValue<string>()
            ?? throw new InvalidOperationException("MESSAGE_TYPE missing.");
        long code = root[UserDataWireNames.Code]?.GetValue<long>() ?? 0;
        string message = root[UserDataWireNames.Message]?.GetValue<string>() ?? string.Empty;
        long ts = root[UserDataWireNames.TimeStamp]?.GetValue<long>() ?? 0;

        Dictionary<string, string> payload = new(StringComparer.Ordinal);
        JsonNode? payloadNode = root[UserDataWireNames.Payload];
        if (payloadNode is JsonObject obj)
        {
            foreach (KeyValuePair<string, JsonNode?> property in obj)
            {
                payload[property.Key] = ReadPayloadString(property.Value);
            }
        }
        else if (payloadNode is not null
            && payloadNode.GetValueKind() == JsonValueKind.String)
        {
            // Tolerate stringified JSON payload (future master-key decrypt output).
            JsonNode? inner = JsonNode.Parse(payloadNode.GetValue<string>());
            if (inner is JsonObject innerObj)
            {
                foreach (KeyValuePair<string, JsonNode?> property in innerObj)
                {
                    payload[property.Key] = ReadPayloadString(property.Value);
                }
            }
        }

        return new UserDataApiFrame(messageType, code, message, ts, payload);
    }

    /// <summary>
    /// Coerces a JSON payload node into a string field value.
    /// Use: High (Decode). Scope: PlainJsonUserDataWireCodec.
    /// </summary>
    private static string ReadPayloadString(JsonNode? node)
    {
        if (node is null)
        {
            return string.Empty;
        }

        return node.GetValueKind() switch
        {
            JsonValueKind.String => node.GetValue<string>() ?? string.Empty,
            JsonValueKind.Number => node.ToJsonString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => node.ToJsonString(),
        };
    }
}
