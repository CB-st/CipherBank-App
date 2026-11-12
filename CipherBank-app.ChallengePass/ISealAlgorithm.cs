// <copyright file="ISealAlgorithm.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Slot 1 — cryptographic seal/open + keypair from seed.
/// Swap to change AEAD/KEM without touching templates or HTTP structure.
/// </summary>
public interface ISealAlgorithm
{
    /// <summary>Wire <c>ALGORITHM</c> value.</summary>
    string AlgorithmId { get; }

    /// <summary>Gets the public key size in bytes.</summary>
    int PublicKeySize { get; }

    /// <summary>Gets the private key size in bytes.</summary>
    int PrivateKeySize { get; }

    /// <summary>
    /// Derives the account keypair from a 32-byte seed. The caller owns both returned key buffers
    /// and must zero the private key after use; the seed is not retained.
    /// Use: Low (identity adoption). Scope: suite seal algorithm.
    /// </summary>
    AccountKeyPair DeriveKeyPair(ReadOnlySpan<byte> seed32);

    /// <summary>
    /// Seals <paramref name="plaintext"/> to <paramref name="recipientPublicKey"/>; returns a
    /// caller-owned wire blob. Use: Medium (pass construction). Scope: suite seal algorithm.
    /// </summary>
    byte[] Seal(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> recipientPublicKey);

    /// <summary>
    /// Opens a sealed blob with the recipient private key; the returned plaintext is caller-owned
    /// and must be wiped when secret. Malformed input throws. Use: Medium (challenge open).
    /// Scope: suite seal algorithm.
    /// </summary>
    byte[] Open(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> recipientPrivateKey);
}
