// <copyright file="ChallengeIdNonceSha256Template.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace CipherBank_app.ChallengePass.Templates;

/// <summary>
/// A1 template slot: <c>P = UTF8(CHALLENGE_ID) || 0x00 || NONCE</c>; pass payload = SHA-256(P).
/// </summary>
public sealed class ChallengeIdNonceSha256Template : IChallengeTemplate
{
    public const string TemplateIdValue = "challenge-id-null-nonce-sha256-v1";

    public string TemplateId => TemplateIdValue;

    public int MinNonceLength => 16;

    public byte[] BuildChallengePlaintext(ChallengeBindContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.ChallengeId))
        {
            throw new ArgumentException("ChallengeId required.", nameof(context));
        }

        if (context.Nonce.Length < MinNonceLength)
        {
            throw new ArgumentException($"Nonce must be at least {MinNonceLength} bytes.", nameof(context));
        }

        byte[] id = Encoding.UTF8.GetBytes(context.ChallengeId);
        var plaintext = new byte[id.Length + 1 + context.Nonce.Length];
        id.CopyTo(plaintext, 0);
        plaintext[id.Length] = 0x00;
        context.Nonce.CopyTo(plaintext.AsSpan(id.Length + 1));
        return plaintext;
    }

    public ParsedChallenge ParseChallengePlaintext(ReadOnlySpan<byte> plaintext)
    {
        int sep = plaintext.IndexOf((byte)0x00);
        if (sep <= 0 || sep >= plaintext.Length - 1)
        {
            throw new CryptographicException("Invalid challenge plaintext framing.");
        }

        string challengeId = Encoding.UTF8.GetString(plaintext[..sep]);
        byte[] nonce = plaintext[(sep + 1)..].ToArray();
        if (nonce.Length < MinNonceLength)
        {
            throw new CryptographicException("Nonce too short in challenge plaintext.");
        }

        return new ParsedChallenge
        {
            ChallengeId = challengeId,
            Nonce = nonce,
            RawPlaintext = plaintext.ToArray(),
        };
    }

    public byte[] BuildPassPayload(ParsedChallenge opened)
    {
        ArgumentNullException.ThrowIfNull(opened);
        return SHA256.HashData(opened.RawPlaintext);
    }
}
