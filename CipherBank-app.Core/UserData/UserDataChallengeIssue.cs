// <copyright file="UserDataChallengeIssue.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Result of CHALLENGE_USER_DATA (RSA-OAEP ciphertext of challenge bytes).</summary>
public sealed class UserDataChallengeIssue
{
    public UserDataChallengeIssue(
        UserDataStatusCode code,
        byte[]? encryptedChallenge,
        DateTimeOffset? expiresAt,
        string? effective2FaMethod,
        string? details = null)
    {
        Code = code;
        EncryptedChallenge = encryptedChallenge;
        ExpiresAt = expiresAt;
        Effective2FaMethod = effective2FaMethod;
        Details = details;
    }

    public UserDataStatusCode Code { get; }

    public byte[]? EncryptedChallenge { get; }

    public DateTimeOffset? ExpiresAt { get; }

    public string? Effective2FaMethod { get; }

    public string? Details { get; }

    public bool IsSuccess => Code == UserDataStatusCode.Ok && EncryptedChallenge is { Length: > 0 };
}
