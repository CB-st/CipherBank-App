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

    int PublicKeySize { get; }

    int PrivateKeySize { get; }

    AccountKeyPair DeriveKeyPair(ReadOnlySpan<byte> seed32);

    byte[] Seal(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> recipientPublicKey);

    byte[] Open(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> recipientPrivateKey);
}
