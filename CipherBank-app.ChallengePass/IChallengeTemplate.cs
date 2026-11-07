// <copyright file="IChallengeTemplate.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>
/// Slot 2 — challenge plaintext framing and pass payload shape.
/// Swap to change CHALLENGE_ID/nonce layout or hash-vs-raw pass without changing crypto.
/// </summary>
public interface IChallengeTemplate
{
    /// <summary>Gets the wire template id.</summary>
    string TemplateId { get; }

    /// <summary>Gets the minimum accepted challenge nonce length in bytes.</summary>
    int MinNonceLength { get; }

    /// <summary>Frames the challenge plaintext for <paramref name="context"/> (caller-owned buffer).</summary>
    byte[] BuildChallengePlaintext(ChallengeBindContext context);

    /// <summary>
    /// Parses opened challenge plaintext; malformed framing or a short nonce throws instead of
    /// producing a partial challenge. Use: Medium (each session proof). Scope: suite template.
    /// </summary>
    ParsedChallenge ParseChallengePlaintext(ReadOnlySpan<byte> plaintext);

    /// <summary>Bytes sealed to the API public key (e.g. SHA-256 of plaintext).</summary>
    byte[] BuildPassPayload(ParsedChallenge opened);
}
