// <copyright file="HttpSessionChallengeClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass;
using CipherBank_app.V1;

namespace CipherBank_app.Services;

/// <summary>Live <c>POST /v1/session/challenge</c> for A1 sealed challenge/pass.</summary>
public sealed class HttpSessionChallengeClient : ISessionChallengeClient
{
    // Lazy breaks DI cycle: HttpProductApi → ISessionProofBuilder → this → IProductApi.
    private readonly Lazy<IProductApi> _api;

    public HttpSessionChallengeClient(Lazy<IProductApi> api) => _api = api;

    public Task<SessionChallengeDto> RequestChallengeAsync(string accountPublicKeyWire, CancellationToken ct = default)
        => _api.Value.CreateSessionChallengeAsync(accountPublicKeyWire, ct);
}
