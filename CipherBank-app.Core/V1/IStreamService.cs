// <copyright file="IStreamService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Product websocket / mock stream.</summary>
public interface IStreamService
{
    /// <summary>Raised for each event received from the product stream.</summary>
    event EventHandler<StreamEventArgs>? EventReceived;

    /// <summary>Gets a value indicating whether the stream is currently connected.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects and starts receiving; <paramref name="ct"/> cancels only the handshake — the
    /// established stream is owned by <see cref="DisconnectAsync"/>.
    /// Use: High (session start). Scope: process stream connection.
    /// </summary>
    Task ConnectAsync(CancellationToken ct);

    /// <summary>
    /// Stops the receive loop and closes the connection; idempotent.
    /// Use: Medium (lock / shutdown). Scope: process stream connection.
    /// </summary>
    Task DisconnectAsync();
}
