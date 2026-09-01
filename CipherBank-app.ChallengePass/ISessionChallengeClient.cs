// <copyright file="ISessionChallengeClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Port for structure slot → product API. Implemented by app/mock without baking HTTP into the module.
/// </summary>
public interface ISessionChallengeClient
{
    Task<SessionChallengeDto> RequestChallengeAsync(string accountPublicKeyWire, CancellationToken ct);

    /// <summary>Requests a challenge without a caller-supplied cancellation token.</summary>
    /// <param name="accountPublicKeyWire">Wire-encoded account public key.</param>
    /// <returns>The issued session challenge.</returns>
    /// <remarks>Use: Medium (every session open). Scope: any ISessionChallengeClient caller.</remarks>
    Task<SessionChallengeDto> RequestChallengeAsync(string accountPublicKeyWire)
        => RequestChallengeAsync(accountPublicKeyWire, CancellationToken.None);
}
