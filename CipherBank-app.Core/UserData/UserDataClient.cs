// <copyright file="UserDataClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;

namespace CipherBank_app.UserData;

/// <summary><see cref="IUserDataClient"/> over an <see cref="IUserDataTransport"/> (TCP or future adapters).</summary>
public sealed class UserDataClient : IUserDataClient
{
    private readonly IUserDataTransport _transport;

    /// <summary>
    /// Wraps a transport. Use: Medium (DI). Scope: userdata Core.
    /// </summary>
    public UserDataClient(IUserDataTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
    }

    /// <inheritdoc />
    public async Task<UserDataEnrollResult> EnrollAsync(string username, string publicKeyPem, CancellationToken ct)
    {
        Dictionary<string, string> payload = new(StringComparer.Ordinal)
        {
            [UserDataWireNames.Username] = username,
            [UserDataWireNames.PublicKeyPem] = publicKeyPem,
        };

        UserDataApiFrame frame = await _transport.ExchangeAsync(UserDataWireNames.EnrollUserRequest, payload, ct)
            .ConfigureAwait(false);
        return new UserDataEnrollResult(ToStatus(frame.Code), GetDetails(frame));
    }

    /// <inheritdoc />
    public async Task<UserDataChallengeIssue> ChallengeAsync(
        string username,
        string preferred2FaMethod,
        CancellationToken ct)
    {
        Dictionary<string, string> payload = new(StringComparer.Ordinal)
        {
            [UserDataWireNames.Username] = username,
            [UserDataWireNames.Preferred2FaMethod] = preferred2FaMethod,
        };

        UserDataApiFrame frame = await _transport
            .ExchangeAsync(UserDataWireNames.ChallengeUserDataRequest, payload, ct)
            .ConfigureAwait(false);

        if (ToStatus(frame.Code) != UserDataStatusCode.Ok)
        {
            return new UserDataChallengeIssue(ToStatus(frame.Code), null, null, null, GetDetails(frame));
        }

        byte[]? encrypted = null;
        if (frame.Payload.TryGetValue(UserDataWireNames.EncryptedChallengeBlob, out string? b64)
            && !string.IsNullOrWhiteSpace(b64))
        {
            encrypted = Convert.FromBase64String(b64);
        }

        DateTimeOffset? expires = null;
        if (frame.Payload.TryGetValue(UserDataWireNames.ExpiresAt, out string? exp)
            && long.TryParse(exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out long unix))
        {
            expires = DateTimeOffset.FromUnixTimeSeconds(unix);
        }

        frame.Payload.TryGetValue(UserDataWireNames.Effective2FaMethod, out string? method);
        return new UserDataChallengeIssue(UserDataStatusCode.Ok, encrypted, expires, method, GetDetails(frame));
    }

    /// <inheritdoc />
    public async Task<UserDataGrabResult> GrabAsync(
        string username,
        ReadOnlyMemory<byte> challengeResponsePlain,
        CancellationToken ct)
    {
        Dictionary<string, string> payload = new(StringComparer.Ordinal)
        {
            [UserDataWireNames.Username] = username,
            [UserDataWireNames.ChallengeResponseBlob] = Convert.ToBase64String(challengeResponsePlain.Span),
        };

        UserDataApiFrame frame = await _transport
            .ExchangeAsync(UserDataWireNames.GrabUserDataRequest, payload, ct)
            .ConfigureAwait(false);

        frame.Payload.TryGetValue(UserDataWireNames.UserDataBlob, out string? blob);
        return new UserDataGrabResult(ToStatus(frame.Code), blob, GetDetails(frame));
    }

    /// <inheritdoc />
    public async Task<UserDataOverwriteResult> OverwriteAsync(
        string username,
        ReadOnlyMemory<byte> challengeResponsePlain,
        string newUserDataBlobBase64,
        CancellationToken ct)
    {
        Dictionary<string, string> payload = new(StringComparer.Ordinal)
        {
            [UserDataWireNames.Username] = username,
            [UserDataWireNames.ChallengeResponseBlob] = Convert.ToBase64String(challengeResponsePlain.Span),
            [UserDataWireNames.NewUserDataBlob] = newUserDataBlobBase64,
            [UserDataWireNames.Overwrite] = bool.TrueString,
            [UserDataWireNames.AreYouSure] = bool.TrueString,
        };

        UserDataApiFrame frame = await _transport
            .ExchangeAsync(UserDataWireNames.OverwriteUserDataRequest, payload, ct)
            .ConfigureAwait(false);

        frame.Payload.TryGetValue(UserDataWireNames.OldUserDataBlob, out string? old);
        return new UserDataOverwriteResult(ToStatus(frame.Code), old, GetDetails(frame));
    }

    private static UserDataStatusCode ToStatus(long code)
        => Enum.IsDefined(typeof(UserDataStatusCode), (int)code)
            ? (UserDataStatusCode)(int)code
            : UserDataStatusCode.TransportFailure;

    private static string? GetDetails(UserDataApiFrame frame)
        => frame.Payload.TryGetValue(UserDataWireNames.Details, out string? details) ? details : null;
}
