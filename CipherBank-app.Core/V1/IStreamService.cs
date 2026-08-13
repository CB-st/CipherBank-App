// <copyright file="IStreamService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Product websocket / mock stream.</summary>
public interface IStreamService
{
    event EventHandler<StreamEventArgs>? EventReceived;

    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken ct);

    Task DisconnectAsync();
}
