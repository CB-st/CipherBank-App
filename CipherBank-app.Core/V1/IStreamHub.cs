// <copyright file="IStreamHub.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Process-wide fan-out of product stream events (one subscription to the socket).</summary>
public interface IStreamHub
{
    /// <summary>Raised for every product stream event fanned out to subscribers.</summary>
    event EventHandler<StreamEventArgs>? EventReceived;

    /// <summary>Gets a value indicating whether the hub is subscribed to the stream service.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Subscribes the hub to the stream service; call before connecting so no early event is lost.
    /// Use: High (session start). Scope: process-wide stream hub.
    /// </summary>
    void Start();

    /// <summary>
    /// Tears down the hub subscription so stream events stop fan-out.
    /// Use: Medium (lock / logout). Scope: process-wide stream hub.
    /// </summary>
    void StopStreaming();
}
