// <copyright file="IProductSessionCoordinator.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Session;

/// <summary>Owns the remote product session, stream, bootstrap, and token store lifecycle.</summary>
public interface IProductSessionCoordinator
{
    Task<ProductSessionStartResult> StartAsync(bool applyBootstrap, CancellationToken ct);

    void StopSession();

    Task DisconnectAsync();
}
