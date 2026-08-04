// <copyright file="IUserDataTransport.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Sends one request frame and returns one response frame (CIPHERBANK_INTERNAL).</summary>
public interface IUserDataTransport
{
    /// <summary>
    /// Exchange a single request/response. Use: High (UserDataClient). Scope: transport.
    /// </summary>
    Task<UserDataApiFrame> ExchangeAsync(
        string requestType,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken ct);
}
