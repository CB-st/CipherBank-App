// <copyright file="IStreamHub.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Process-wide fan-out of product stream events (one subscription to the socket).</summary>
public interface IStreamHub
{
    event EventHandler<StreamEventArgs>? EventReceived;

    bool IsRunning { get; }

    void Start();

    /// <summary>
    /// Tears down the hub subscription so stream events stop fan-out.
    /// Use: Medium (lock / logout). Scope: process-wide stream hub.
    /// </summary>
    void StopStreaming();
}
