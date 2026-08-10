// <copyright file="HybridIdentityBuffers.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>
/// Shared wipe helper for HybridPrivateIdentity buffer arrays.
/// Use: Medium (clear / cancel / swap paths). Scope: ChallengePass hybrid wipe contract.
/// </summary>
internal static class HybridIdentityBuffers
{
    /// <summary>
    /// Overwrites X25519 and ML-KEM public/private buffers with zeros.
    /// Use: Medium. Scope: hybrid identity wipe helpers.
    /// </summary>
    internal static void Zero(HybridPrivateIdentity identity)
    {
        CryptographicOperations.ZeroMemory(identity.X25519PrivateKey);
        CryptographicOperations.ZeroMemory(identity.MlKemPrivateKey);
        CryptographicOperations.ZeroMemory(identity.X25519PublicKey);
        CryptographicOperations.ZeroMemory(identity.MlKemPublicKey);
    }
}
