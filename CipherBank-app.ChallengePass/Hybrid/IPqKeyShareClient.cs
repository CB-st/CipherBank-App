// <copyright file="IPqKeyShareClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>Port for server-side key share (HTTP later; in-memory for tests).</summary>
public interface IPqKeyShareClient
{
    Task<PqKeyShareResponse> EstablishAsync(HybridPublicIdentity device, CancellationToken ct);

    /// <summary>Establishes a key share without a caller-supplied cancellation token.</summary>
    /// <param name="device">Device hybrid public identity.</param>
    /// <returns>The server key-share response.</returns>
    /// <remarks>Use: Low (once per unlock). Scope: any IPqKeyShareClient caller.</remarks>
    Task<PqKeyShareResponse> EstablishAsync(HybridPublicIdentity device)
        => EstablishAsync(device, CancellationToken.None);
}
