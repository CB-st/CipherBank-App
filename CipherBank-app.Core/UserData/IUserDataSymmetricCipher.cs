// <copyright file="IUserDataSymmetricCipher.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Internal symmetric AEAD primitive used by pack blocks and other Core helpers.
/// Swap only with an explicit algorithm-id / pack-format bump.
/// </summary>
public interface IUserDataSymmetricCipher
{
    string AlgorithmId { get; }

    /// <summary>
    /// Seals plaintext under a 32-byte key with optional AAD (random nonce).
    /// Use: High (pack + internal wrappers). Scope: userdata crypto suite.
    /// </summary>
    UserDataSymmetricBlob Seal(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key32,
        ReadOnlySpan<byte> aad);

    /// <summary>
    /// Opens a sealed blob; authentication failure throws <see cref="System.Security.Cryptography.CryptographicException"/>.
    /// Use: High (pack + internal wrappers). Scope: userdata crypto suite.
    /// </summary>
    byte[] Open(
        UserDataSymmetricBlob blob,
        ReadOnlySpan<byte> key32,
        ReadOnlySpan<byte> aad);

    /// <summary>
    /// Seals with an injected nonce (test vectors / deterministic fixtures only).
    /// Use: Low (unit tests). Scope: userdata crypto suite.
    /// </summary>
    UserDataSymmetricBlob SealWithNonce(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key32,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> nonce12);
}
