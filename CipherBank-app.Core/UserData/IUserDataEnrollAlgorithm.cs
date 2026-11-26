// <copyright file="IUserDataEnrollAlgorithm.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Swappable enroll / challenge possession algorithm (RSA-OAEP v1 today; PQ-capable suite later).
/// Separate key domain from ChallengePass Hybrid session crypto.
/// </summary>
public interface IUserDataEnrollAlgorithm
{
    /// <summary>Stable algorithm id recorded on suites and future wire metadata.</summary>
    string AlgorithmId { get; }

    /// <summary>
    /// Rematerializes a deterministic keypair from the mnemonic-derived enroll-seed.
    /// Use: High (unlock / enroll). Scope: userdata enroll slot.
    /// </summary>
    UserDataEnrollKeyPair DeriveKeyPair(ReadOnlySpan<byte> enrollSeed64);

    /// <summary>
    /// Decrypts an RSA-OAEP (or future) challenge ciphertext to the raw challenge bytes.
    /// Use: High (CHALLENGE response). Scope: userdata client.
    /// </summary>
    byte[] DecryptChallenge(ReadOnlySpan<byte> encryptedChallenge, UserDataEnrollKeyPair keys);

    /// <summary>
    /// Encrypts a challenge under the public PEM (tests + future re-encrypt helpers).
    /// Use: Low (tests / tooling). Scope: userdata enroll slot.
    /// </summary>
    byte[] EncryptChallenge(ReadOnlySpan<byte> challengePlaintext, string publicKeyPem);
}
