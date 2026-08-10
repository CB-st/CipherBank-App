// <copyright file="HybridPrivateIdentity.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass.Hybrid;

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
