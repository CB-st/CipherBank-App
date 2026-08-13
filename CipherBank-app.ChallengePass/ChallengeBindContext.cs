// <copyright file="ChallengeBindContext.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>Binding inputs for building challenge plaintext.</summary>
public sealed class ChallengeBindContext
{
    public required string ChallengeId { get; init; }

    public required byte[] Nonce { get; init; }

    public string? DeviceId { get; init; }
}
