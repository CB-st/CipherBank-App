// <copyright file="IPqChannelChallengeSource.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>Issues challenges sealed with the shared PQ channel key (server side).</summary>
public interface IPqChannelChallengeSource
{
    Task<SessionChallengeDto> RequestChallengeAsync(CancellationToken ct);

    /// <summary>Requests a sealed challenge without a caller-supplied cancellation token.</summary>
    /// <returns>The issued session challenge.</returns>
    /// <remarks>Use: Medium (every session open). Scope: any IPqChannelChallengeSource caller.</remarks>
    Task<SessionChallengeDto> RequestChallengeAsync()
        => RequestChallengeAsync(CancellationToken.None);
}
