// <copyright file="UnavailableStreamService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>Allows login composition while refusing sessions without a configured secure stream.</summary>
public sealed class UnavailableStreamService : IStreamService
{
    /// <inheritdoc />
    public event EventHandler<StreamEventArgs>? EventReceived
    {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public bool IsConnected => false;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Fail at use, preserving the secure endpoint requirement without breaking login DI.
        throw new InvalidOperationException("ProductSession:StreamEndpoint must be an absolute wss URL.");
    }

    /// <inheritdoc />
    public Task DisconnectAsync() => Task.CompletedTask;
}
