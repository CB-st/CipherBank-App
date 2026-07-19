// <copyright file="HybridKeyShareModels.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>Device hybrid identity published at key-share time (public only on the wire).</summary>
public sealed class HybridPublicIdentity
{
    public required string X25519PublicKeyWire { get; init; }

    public required string MlKemPublicKeyWire { get; init; }

    public string Algorithm { get; init; } = HybridMlKemX25519Agreement.KeyShareAlgorithmId;
}

/// <summary>Device private hybrid material (memory only while unlocked).</summary>
public sealed class HybridPrivateIdentity
{
    public required byte[] X25519PublicKey { get; init; }

    public required byte[] X25519PrivateKey { get; init; }

    public required byte[] MlKemPublicKey { get; init; }

    public required byte[] MlKemPrivateKey { get; init; }

    public HybridPublicIdentity ToPublic() => new()
    {
        X25519PublicKeyWire = WireEncoding.ToWire(X25519PublicKey),
        MlKemPublicKeyWire = WireEncoding.ToWire(MlKemPublicKey),
    };
}

/// <summary>Server → device key-share message after hybrid encapsulation.</summary>
public sealed class PqKeyShareResponse
{
    public required string KeyShareId { get; init; }

    public required string MlKemCiphertextWire { get; init; }

    public required string ServerX25519PublicKeyWire { get; init; }

    public string Algorithm { get; init; } = HybridMlKemX25519Agreement.KeyShareAlgorithmId;
}

/// <summary>Port for server-side key share (HTTP later; in-memory for tests).</summary>
public interface IPqKeyShareClient
{
    Task<PqKeyShareResponse> EstablishAsync(HybridPublicIdentity device, CancellationToken ct = default);
}
