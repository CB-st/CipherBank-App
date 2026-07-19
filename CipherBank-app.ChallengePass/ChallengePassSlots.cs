// <copyright file="ChallengePassSlots.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>Wire-facing account keypair (private only while unlocked).</summary>
public readonly record struct AccountKeyPair(byte[] PublicKey, byte[] PrivateKey);

/// <summary>Binding inputs for building challenge plaintext.</summary>
public sealed class ChallengeBindContext
{
    public required string ChallengeId { get; init; }

    public required byte[] Nonce { get; init; }

    public string? DeviceId { get; init; }
}

/// <summary>Parsed challenge after opening ciphertext.</summary>
public sealed class ParsedChallenge
{
    public required string ChallengeId { get; init; }

    public required byte[] Nonce { get; init; }

    public required byte[] RawPlaintext { get; init; }
}

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

/// <summary>
/// Slot 2 — challenge plaintext framing and pass payload shape.
/// Swap to change CHALLENGE_ID/nonce layout or hash-vs-raw pass without changing crypto.
/// </summary>
public interface IChallengeTemplate
{
    string TemplateId { get; }

    int MinNonceLength { get; }

    byte[] BuildChallengePlaintext(ChallengeBindContext context);

    ParsedChallenge ParseChallengePlaintext(ReadOnlySpan<byte> plaintext);

    /// <summary>Bytes sealed to the API public key (e.g. SHA-256 of plaintext).</summary>
    byte[] BuildPassPayload(ParsedChallenge opened);
}

/// <summary>
/// Slot 3 — protocol structure (round-trips, request/response mapping).
/// Swap to change challenge→pass HTTP choreography without changing crypto or framing.
/// </summary>
public interface IChallengePassStructure
{
    string StructureId { get; }

    Task<object> BuildSessionOpenBodyAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire,
        CancellationToken ct = default);
}

/// <summary>Composed, named suite: one plugged algorithm + template + structure.</summary>
public sealed class ChallengePassSuite
{
    public ChallengePassSuite(
        string suiteId,
        ISealAlgorithm algorithm,
        IChallengeTemplate template,
        IChallengePassStructure structure)
    {
        SuiteId = suiteId;
        Algorithm = algorithm;
        Template = template;
        Structure = structure;
    }

    public string SuiteId { get; }

    public ISealAlgorithm Algorithm { get; }

    public IChallengeTemplate Template { get; }

    public IChallengePassStructure Structure { get; }
}

/// <summary>Registry of installed suites; active suite is slot-selected by id.</summary>
public interface IChallengePassCatalog
{
    string ActiveSuiteId { get; }

    IReadOnlyList<string> AvailableSuiteIds { get; }

    ChallengePassSuite GetActive();

    ChallengePassSuite GetSuite(string suiteId);

    /// <summary>Slot-out / slot-in the active suite without rebuilding DI.</summary>
    void SetActive(string suiteId);
}
