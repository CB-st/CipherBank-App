// <copyright file="IStreamService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Product websocket / mock stream.</summary>
public interface IStreamService
{
    event EventHandler<StreamEvent>? EventReceived;

    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken ct);

    Task DisconnectAsync();
}
