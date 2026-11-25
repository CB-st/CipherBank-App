// <copyright file="IProductSessionCoordinator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Session;

/// <summary>Owns the remote product session, stream, bootstrap, and token store lifecycle.</summary>
public interface IProductSessionCoordinator
{
    /// <summary>
    /// Creates the product session (proof, tokens, prefs pull, optional bootstrap) and starts the
    /// stream. Local prefs load before the remote refresh so a failed pull never resets lock policy.
    /// Use: High (each unlock). Scope: process-wide product session.
    /// </summary>
    Task<ProductSessionStartResult> StartAsync(bool applyBootstrap, CancellationToken ct);

    /// <summary>
    /// Drops tokens and stops the stream synchronously (lock path; no network).
    /// Use: High (lock). Scope: process-wide product session.
    /// </summary>
    void StopSession();

    /// <summary>
    /// Gracefully closes the stream connection (logout / shutdown paths).
    /// Use: Low (shutdown). Scope: process-wide product session.
    /// </summary>
    Task DisconnectAsync();
}
