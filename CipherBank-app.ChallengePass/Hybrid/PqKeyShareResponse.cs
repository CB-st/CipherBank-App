// <copyright file="PqKeyShareResponse.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>Server → device key-share message after hybrid encapsulation.</summary>
public sealed class PqKeyShareResponse
{
    public required string KeyShareId { get; init; }

    public required string MlKemCiphertextWire { get; init; }

    public required string ServerX25519PublicKeyWire { get; init; }

    public string Algorithm { get; init; } = HybridMlKemX25519Agreement.KeyShareAlgorithmId;
}
