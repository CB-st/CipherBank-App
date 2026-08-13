// <copyright file="IChallengeTemplate.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass;

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
