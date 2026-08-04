// <copyright file="MockUserDataClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>In-process <see cref="IUserDataClient"/> backed by <see cref="UserDataServiceLogic"/> (no TCP).</summary>
public sealed class MockUserDataClient : IUserDataClient
{
    private readonly UserDataServiceLogic _logic;

    /// <summary>
    /// Creates a mock with a fresh store. Use: High (unit tests). Scope: test composition.
    /// </summary>
    public MockUserDataClient()
        : this(new UserDataServiceLogic(new InMemoryUserDataStore()))
    {
    }

    /// <summary>
    /// Shares a store with a loopback server for cross-substantiation. Use: Medium. Scope: tests.
    /// </summary>
    public MockUserDataClient(UserDataServiceLogic logic)
    {
        ArgumentNullException.ThrowIfNull(logic);
        _logic = logic;
    }

    /// <summary>Exposes the underlying service for loopback pairing. Use: Low (tests). Scope: mock.</summary>
    public UserDataServiceLogic Logic => _logic;

    /// <inheritdoc />
    public Task<UserDataEnrollResult> EnrollAsync(string username, string publicKeyPem, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_logic.Enroll(username, publicKeyPem));
    }

    /// <inheritdoc />
    public Task<UserDataChallengeIssue> ChallengeAsync(string username, string preferred2FaMethod, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_logic.Challenge(username, preferred2FaMethod));
    }

    /// <inheritdoc />
    public Task<UserDataGrabResult> GrabAsync(
        string username,
        ReadOnlyMemory<byte> challengeResponsePlain,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_logic.Grab(username, challengeResponsePlain.Span));
    }

    /// <inheritdoc />
    public Task<UserDataOverwriteResult> OverwriteAsync(
        string username,
        ReadOnlyMemory<byte> challengeResponsePlain,
        string newUserDataBlobBase64,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(
            _logic.Overwrite(username, challengeResponsePlain.Span, newUserDataBlobBase64, overwrite: true, areYouSure: true));
    }
}
