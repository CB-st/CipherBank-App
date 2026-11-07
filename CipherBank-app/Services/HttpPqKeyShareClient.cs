// <copyright file="HttpPqKeyShareClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;
using CipherBank_app.V1;

namespace CipherBank_app.Services;

/// <summary>Live <c>POST /v1/session/key-share</c> for A2 hybrid PQ channel establishment.</summary>
public sealed class HttpPqKeyShareClient : IPqKeyShareClient
{
    // Lazy breaks DI cycle: HttpProductClient → ISessionProofBuilder → A2 → this → IProductClient.
    private readonly Lazy<IProductClient> _api;

    public HttpPqKeyShareClient(Lazy<IProductClient> api) => _api = api;

    public async Task<PqKeyShareResponse> EstablishAsync(HybridPublicIdentity device, CancellationToken ct = default)
    {
        KeyShareResponseDto dto = await _api.Value.EstablishKeyShareAsync(
            new KeyShareRequestDto
            {
                X25519PublicKey = device.X25519PublicKeyWire,
                MlKemPublicKey = device.MlKemPublicKeyWire,
                Algorithm = device.Algorithm,
            },
            ct).ConfigureAwait(false);

        return new PqKeyShareResponse
        {
            KeyShareId = dto.KeyShareId,
            MlKemCiphertextWire = dto.MlKemCiphertext,
            ServerX25519PublicKeyWire = dto.ServerX25519PublicKey,
            Algorithm = dto.Algorithm,
        };
    }
}
