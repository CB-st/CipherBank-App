// <copyright file="IUserDataClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Port for CipherBank-src user_data ENROLL / CHALLENGE / GRAB / OVERWRITE.
/// Transport (mock, TCP loopback, or production) is injected behind the scenes.
/// </summary>
public interface IUserDataClient
{
    Task<UserDataEnrollResult> EnrollAsync(string username, string publicKeyPem, CancellationToken ct);

    /// <summary>Zero-token DIM forwarder. Use: Medium. Scope: IUserDataClient consumers.</summary>
    Task<UserDataEnrollResult> EnrollAsync(string username, string publicKeyPem)
        => EnrollAsync(username, publicKeyPem, CancellationToken.None);

    Task<UserDataChallengeIssue> ChallengeAsync(string username, string preferred2FaMethod, CancellationToken ct);

    /// <summary>Zero-token DIM forwarder. Use: Medium. Scope: IUserDataClient consumers.</summary>
    Task<UserDataChallengeIssue> ChallengeAsync(string username, string preferred2FaMethod)
        => ChallengeAsync(username, preferred2FaMethod, CancellationToken.None);

    Task<UserDataGrabResult> GrabAsync(
        string username,
        ReadOnlyMemory<byte> challengeResponsePlain,
        CancellationToken ct);

    /// <summary>Zero-token DIM forwarder. Use: Medium. Scope: IUserDataClient consumers.</summary>
    Task<UserDataGrabResult> GrabAsync(string username, ReadOnlyMemory<byte> challengeResponsePlain)
        => GrabAsync(username, challengeResponsePlain, CancellationToken.None);

    Task<UserDataOverwriteResult> OverwriteAsync(
        string username,
        ReadOnlyMemory<byte> challengeResponsePlain,
        string newUserDataBlobBase64,
        CancellationToken ct);

    /// <summary>Zero-token DIM forwarder. Use: Medium. Scope: IUserDataClient consumers.</summary>
    Task<UserDataOverwriteResult> OverwriteAsync(
        string username,
        ReadOnlyMemory<byte> challengeResponsePlain,
        string newUserDataBlobBase64)
        => OverwriteAsync(username, challengeResponsePlain, newUserDataBlobBase64, CancellationToken.None);
}
