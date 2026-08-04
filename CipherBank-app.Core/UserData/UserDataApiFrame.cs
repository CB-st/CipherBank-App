// <copyright file="UserDataApiFrame.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Decoded CIPHERBANK_INTERNAL userdata frame (superstructure + payload map).</summary>
public sealed class UserDataApiFrame
{
    public UserDataApiFrame(
        string messageType,
        long code,
        string message,
        long timeStampUnix,
        Dictionary<string, string> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(payload);
        MessageType = messageType;
        Code = code;
        Message = message ?? string.Empty;
        TimeStampUnix = timeStampUnix;
        Payload = payload;
    }

    public string MessageType { get; }

    public long Code { get; }

    public string Message { get; }

    public long TimeStampUnix { get; }

    public Dictionary<string, string> Payload { get; }
}
