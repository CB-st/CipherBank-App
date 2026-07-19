// <copyright file="InMemoryPqKeyShareClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>In-process key-share server for tests / mock mode; retains channel key for challenge sealing.</summary>
public sealed class InMemoryPqKeyShareClient : IPqKeyShareClient
{
    private readonly HybridMlKemX25519Agreement _agreement = new();

    public byte[]? LastChannelKey { get; private set; }

    public string? LastKeyShareId { get; private set; }

    public Task<PqKeyShareResponse> EstablishAsync(HybridPublicIdentity device, CancellationToken ct = default)
    {
        (PqKeyShareResponse response, byte[] channelKey) = _agreement.CreateShareAsServer(device);
        LastChannelKey = channelKey;
        LastKeyShareId = response.KeyShareId;
        return Task.FromResult(response);
    }
}
