// <copyright file="InMemoryPqKeyShareClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>In-process key-share server for tests / mock mode; retains channel key for challenge sealing.</summary>
public sealed class InMemoryPqKeyShareClient : IPqKeyShareClient, IDisposable
{
    public byte[]? LastChannelKey { get; private set; }

    public string? LastKeyShareId { get; private set; }

    public int EstablishCount { get; private set; }

    public Task<PqKeyShareResponse> EstablishAsync(HybridPublicIdentity device)
        => EstablishAsync(device, CancellationToken.None);

    /// <summary>
    /// Establishes a share and zeroes any previously retained channel key before replacement.
    /// Use: High (tests / mock establish). Scope: InMemoryPqKeyShareClient instance.
    /// </summary>
    public Task<PqKeyShareResponse> EstablishAsync(HybridPublicIdentity device, CancellationToken ct)
    {
        (PqKeyShareResponse response, byte[]? channelKey) = HybridMlKemX25519Agreement.CreateShareAsServer(device);
        ClearLastChannelKey();
        LastChannelKey = channelKey;
        LastKeyShareId = response.KeyShareId;
        EstablishCount++;
        return Task.FromResult(response);
    }

    /// <summary>
    /// Zeroes and drops the retained channel key (call when the mock channel is done).
    /// Use: Medium (test teardown / re-establish). Scope: InMemoryPqKeyShareClient instance.
    /// </summary>
    public void ClearLastChannelKey()
    {
        if (LastChannelKey is not null)
        {
            CryptographicOperations.ZeroMemory(LastChannelKey);
            LastChannelKey = null;
        }
    }

    /// <summary>
    /// Clears retained secrets when the client leaves DI / test scope.
    /// Use: Low (dispose). Scope: InMemoryPqKeyShareClient instance.
    /// </summary>
    public void Dispose() => ClearLastChannelKey();
}
