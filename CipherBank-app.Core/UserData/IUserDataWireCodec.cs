// <copyright file="IUserDataWireCodec.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Encodes/decodes CIPHERBANK_INTERNAL frames (plain or master-key encrypted payload).</summary>
public interface IUserDataWireCodec
{
    UserDataPayloadMode Mode { get; }

    /// <summary>
    /// Builds a wire frame UTF-8 text (without EOF marker). Use: High (TCP send). Scope: transport.
    /// </summary>
    string Encode(string messageType, long code, string message, IReadOnlyDictionary<string, string> payload);

    /// <summary>
    /// Parses a wire frame UTF-8 text (EOF already stripped). Use: High (TCP recv). Scope: transport.
    /// </summary>
    UserDataApiFrame Decode(string frameText);
}
