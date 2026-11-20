// <copyright file="HybridPublicIdentity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>Device hybrid identity published at key-share time (public only on the wire).</summary>
public sealed class HybridPublicIdentity
{
    public required string X25519PublicKeyWire { get; init; }

    public required string MlKemPublicKeyWire { get; init; }

    public string Algorithm { get; init; } = HybridMlKemX25519Agreement.KeyShareAlgorithmId;
}
