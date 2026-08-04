// <copyright file="UserDataChallengeRecord.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Pending challenge hash + TTL for grab/overwrite validation.</summary>
public sealed class UserDataChallengeRecord
{
    public UserDataChallengeRecord(string challengeHashHex, DateTimeOffset expiresAtUtc)
    {
        ChallengeHashHex = challengeHashHex;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string ChallengeHashHex { get; }

    public DateTimeOffset ExpiresAtUtc { get; }
}
