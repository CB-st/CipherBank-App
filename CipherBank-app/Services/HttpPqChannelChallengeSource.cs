// <copyright file="HttpPqChannelChallengeSource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.V1;

namespace CipherBank_app.Services;

/// <summary>
/// Live challenge issuer for A2: server seals with the channel key from the prior key-share.
/// Uses <c>POST /v1/session/challenge</c> with empty account key (server binds via KEY_SHARE session).
/// </summary>
public sealed class HttpPqChannelChallengeSource : IPqChannelChallengeSource
{
    // Lazy breaks DI cycle with HttpProductApi ↔ challenge/pass builders.
    private readonly Lazy<IProductApi> _api;
    private readonly IPqChannel _channel;

    public HttpPqChannelChallengeSource(Lazy<IProductApi> api, IPqChannel channel)
    {
        _api = api;
        _channel = channel;
    }

    public Task<SessionChallengeDto> RequestChallengeAsync(CancellationToken ct = default)
    {
        string bind = _channel.KeyShareId ?? string.Empty;
        return _api.Value.CreateSessionChallengeAsync(bind, ct);
    }
}
