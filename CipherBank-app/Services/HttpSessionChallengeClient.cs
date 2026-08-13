// <copyright file="HttpSessionChallengeClient.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.ChallengePass;
using CipherBank_app.V1;

namespace CipherBank_app.Services;

/// <summary>Live <c>POST /v1/session/challenge</c> for A1 sealed challenge/pass.</summary>
public sealed class HttpSessionChallengeClient : ISessionChallengeClient
{
    // Lazy breaks DI cycle: HttpProductApi → ISessionProofBuilder → this → IProductClient.
    private readonly Lazy<IProductClient> _api;

    public HttpSessionChallengeClient(Lazy<IProductClient> api) => _api = api;

    public Task<SessionChallengeDto> RequestChallengeAsync(string accountPublicKeyWire, CancellationToken ct = default)
        => _api.Value.CreateSessionChallengeAsync(accountPublicKeyWire, ct);
}
