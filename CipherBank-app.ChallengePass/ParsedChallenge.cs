// <copyright file="ParsedChallenge.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>Parsed challenge after opening ciphertext.</summary>
public sealed class ParsedChallenge
{
    public required string ChallengeId { get; init; }

    public required byte[] Nonce { get; init; }

    public required byte[] RawPlaintext { get; init; }
}
