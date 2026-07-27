// <copyright file="IStreamHub.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Process-wide fan-out of product stream events (one subscription to the socket).</summary>
public interface IStreamHub
{
    event EventHandler<StreamEvent>? EventReceived;

    bool IsRunning { get; }

    void Start();

    void Stop();
}
