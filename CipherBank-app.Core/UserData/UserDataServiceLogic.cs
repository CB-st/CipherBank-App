// <copyright file="UserDataServiceLogic.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;

namespace CipherBank_app.UserData;

/// <summary>
/// Shared enroll/challenge/grab/overwrite logic for Mock and loopback self-server.
/// Speaks the same status codes as CipherBank-src UserData_Handler.
/// </summary>
public sealed class UserDataServiceLogic
{
    public const int DefaultChallengeSizeBytes = 96;
    public const int DefaultChallengeTtlSeconds = 300;

    private readonly InMemoryUserDataStore _store;
    private readonly IUserDataEnrollAlgorithm _enroll;
    private readonly TimeProvider _time;
    private readonly int _challengeSizeBytes;
    private readonly int _challengeTtlSeconds;

    /// <summary>
    /// Builds service logic over a shared store. Use: Low (composition). Scope: userdata Core.
    /// </summary>
    public UserDataServiceLogic(
        InMemoryUserDataStore store,
        IUserDataEnrollAlgorithm? enroll = null,
        TimeProvider? timeProvider = null,
        int challengeSizeBytes = DefaultChallengeSizeBytes,
        int challengeTtlSeconds = DefaultChallengeTtlSeconds)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _enroll = enroll ?? new RsaOaepSha256UserDataEnrollAlgorithm();
        _time = timeProvider ?? TimeProvider.System;
        _challengeSizeBytes = challengeSizeBytes;
        _challengeTtlSeconds = challengeTtlSeconds;
    }

    /// <summary>
    /// ENROLL_USER. Use: Medium. Scope: mock / loopback.
    /// </summary>
    public UserDataEnrollResult Enroll(string username, string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(publicKeyPem))
        {
            return new UserDataEnrollResult(UserDataStatusCode.InvalidPublicKey, "Username or public key missing.");
        }

        if (!UserDataServicePayloadHelpers.LooksLikePublicKeyPem(publicKeyPem))
        {
            return new UserDataEnrollResult(UserDataStatusCode.InvalidPublicKey, "Public key PEM could not be recognized.");
        }

        string normalized = UserDataUsernameHash.NormalizeUsername(username);
        string hash = UserDataUsernameHash.HashHex(normalized);
        var identity = new UserDataIdentityRecord(normalized, hash, publicKeyPem);
        return _store.TryAddIdentity(identity)
            ? new UserDataEnrollResult(UserDataStatusCode.Ok)
            : new UserDataEnrollResult(UserDataStatusCode.UsernameExists, "The requested username is already present.");
    }

    /// <summary>
    /// CHALLENGE_USER_DATA. Use: High. Scope: mock / loopback.
    /// </summary>
    public UserDataChallengeIssue Challenge(string username, string preferred2FaMethod)
    {
        if (!TryResolveIdentity(username, out UserDataIdentityRecord? identity, out UserDataStatusCode missing))
        {
            return new UserDataChallengeIssue(missing, null, null, null, "User not found.");
        }

        string effective = UserDataServicePayloadHelpers.Normalize2Fa(preferred2FaMethod);
        if (effective == "INVALID")
        {
            return new UserDataChallengeIssue(
                UserDataStatusCode.InvalidTwoFaMethod,
                null,
                null,
                null,
                "The requested two factor method is not supported.");
        }

        try
        {
            byte[] challenge = RandomNumberGenerator.GetBytes(_challengeSizeBytes);
            string hashHex = Convert.ToHexString(SHA256.HashData(challenge)).ToLowerInvariant();
            DateTimeOffset expires = _time.GetUtcNow().AddSeconds(_challengeTtlSeconds);
            byte[] encrypted = _enroll.EncryptChallenge(challenge, identity!.PublicKeyPem);
            _store.SetChallenge(identity.UsernameHashHex, new UserDataChallengeRecord(hashHex, expires));
            CryptographicOperations.ZeroMemory(challenge);
            return new UserDataChallengeIssue(UserDataStatusCode.Ok, encrypted, expires, effective);
        }
        catch (Exception ex)
        {
            return new UserDataChallengeIssue(
                UserDataStatusCode.CryptographicFailure,
                null,
                null,
                null,
                ex.Message);
        }
    }

    /// <summary>
    /// GRAB_USER_DATA. Use: High. Scope: mock / loopback.
    /// </summary>
    public UserDataGrabResult Grab(string username, ReadOnlySpan<byte> challengeResponsePlain)
    {
        UserDataStatusCode validation = ValidateChallenge(username, challengeResponsePlain, out UserDataIdentityRecord? identity);
        if (validation != UserDataStatusCode.Ok)
        {
            return new UserDataGrabResult(validation, null);
        }

        string blob = _store.GetStash(identity!.UsernameHashHex);
        _store.ClearChallenge(identity.UsernameHashHex);
        return new UserDataGrabResult(UserDataStatusCode.Ok, blob);
    }

    /// <summary>
    /// OVERWRITE_USER_DATA. Use: High. Scope: mock / loopback.
    /// </summary>
    public UserDataOverwriteResult Overwrite(
        string username,
        ReadOnlySpan<byte> challengeResponsePlain,
        string newUserDataBlobBase64,
        bool overwrite,
        bool areYouSure)
    {
        if (!overwrite || !areYouSure)
        {
            return new UserDataOverwriteResult(UserDataStatusCode.OverwriteNotConfirmed, null);
        }

        ArgumentNullException.ThrowIfNull(newUserDataBlobBase64);
        UserDataStatusCode validation = ValidateChallenge(username, challengeResponsePlain, out UserDataIdentityRecord? identity);
        if (validation != UserDataStatusCode.Ok)
        {
            return new UserDataOverwriteResult(validation, null);
        }

        string old = _store.ReplaceStash(identity!.UsernameHashHex, newUserDataBlobBase64);
        _store.ClearChallenge(identity.UsernameHashHex);
        return new UserDataOverwriteResult(UserDataStatusCode.Ok, old);
    }

    /// <summary>
    /// Dispatches a typed request map to the matching operation (loopback codec path).
    /// Use: High (loopback). Scope: UserDataServiceLogic.
    /// </summary>
    public Dictionary<string, string> HandleRequest(string messageType, IReadOnlyDictionary<string, string> payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(payload);

        return messageType switch
        {
            UserDataWireNames.EnrollUserRequest => HandleEnrollRequest(payload),
            UserDataWireNames.ChallengeUserDataRequest => HandleChallengeRequest(payload),
            UserDataWireNames.GrabUserDataRequest => HandleGrabRequest(payload),
            UserDataWireNames.OverwriteUserDataRequest => HandleOverwriteRequest(payload),
            _ => UserDataServiceResponseFactory.Error(UserDataStatusCode.UnknownRequest, "Unknown request."),
        };
    }

    private Dictionary<string, string> HandleEnrollRequest(IReadOnlyDictionary<string, string> payload)
    {
        UserDataEnrollResult result = Enroll(
            UserDataServicePayloadHelpers.GetRequired(payload, UserDataWireNames.Username),
            UserDataServicePayloadHelpers.GetRequired(payload, UserDataWireNames.PublicKeyPem));
        return UserDataServiceResponseFactory.Create(
            UserDataWireNames.EnrollUserResponse,
            result.Code,
            result.Details,
            new Dictionary<string, string>(StringComparer.Ordinal));
    }

    private Dictionary<string, string> HandleChallengeRequest(IReadOnlyDictionary<string, string> payload)
    {
        string username = UserDataServicePayloadHelpers.GetRequired(payload, UserDataWireNames.Username);
        string preferred = UserDataServicePayloadHelpers.GetOptional(payload, UserDataWireNames.Preferred2FaMethod)
            ?? UserDataWireNames.TwoFaUnspecified;
        UserDataChallengeIssue result = Challenge(username, preferred);

        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        if (result.IsSuccess)
        {
            fields[UserDataWireNames.EncryptedChallengeBlob] = Convert.ToBase64String(result.EncryptedChallenge!);
            fields[UserDataWireNames.ExpiresAt] = result.ExpiresAt!.Value.ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture);
            fields[UserDataWireNames.Effective2FaMethod] = result.Effective2FaMethod ?? UserDataWireNames.TwoFaEmail;
        }

        return UserDataServiceResponseFactory.Create(
            UserDataWireNames.ChallengeUserDataResponse,
            result.Code,
            result.Details,
            fields);
    }

    private Dictionary<string, string> HandleGrabRequest(IReadOnlyDictionary<string, string> payload)
    {
        byte[] challenge = UserDataServicePayloadHelpers.DecodeChallengeResponse(
            UserDataServicePayloadHelpers.GetRequired(payload, UserDataWireNames.ChallengeResponseBlob));
        UserDataGrabResult result = Grab(
            UserDataServicePayloadHelpers.GetRequired(payload, UserDataWireNames.Username),
            challenge);
        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        if (result.IsSuccess)
        {
            fields[UserDataWireNames.UserDataBlob] = result.UserDataBlobBase64 ?? string.Empty;
        }

        return UserDataServiceResponseFactory.Create(
            UserDataWireNames.GrabUserDataResponse,
            result.Code,
            result.Details,
            fields);
    }

    private Dictionary<string, string> HandleOverwriteRequest(IReadOnlyDictionary<string, string> payload)
    {
        byte[] challenge = UserDataServicePayloadHelpers.DecodeChallengeResponse(
            UserDataServicePayloadHelpers.GetRequired(payload, UserDataWireNames.ChallengeResponseBlob));
        bool overwrite = UserDataServicePayloadHelpers.ParseBool(
            UserDataServicePayloadHelpers.GetOptional(payload, UserDataWireNames.Overwrite));
        bool sure = UserDataServicePayloadHelpers.ParseBool(
            UserDataServicePayloadHelpers.GetOptional(payload, UserDataWireNames.AreYouSure));
        UserDataOverwriteResult result = Overwrite(
            UserDataServicePayloadHelpers.GetRequired(payload, UserDataWireNames.Username),
            challenge,
            UserDataServicePayloadHelpers.GetRequired(payload, UserDataWireNames.NewUserDataBlob),
            overwrite,
            sure);

        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        if (result.IsSuccess)
        {
            fields[UserDataWireNames.OldUserDataBlob] = result.OldUserDataBlobBase64 ?? string.Empty;
        }

        return UserDataServiceResponseFactory.Create(
            UserDataWireNames.OverwriteUserDataResponse,
            result.Code,
            result.Details,
            fields);
    }

    private UserDataStatusCode ValidateChallenge(
        string username,
        ReadOnlySpan<byte> challengeResponsePlain,
        out UserDataIdentityRecord? identity)
    {
        identity = null;
        if (challengeResponsePlain.Length == 0)
        {
            return UserDataStatusCode.InvalidChallenge;
        }

        if (!TryResolveIdentity(username, out identity, out UserDataStatusCode missing))
        {
            return missing;
        }

        if (!_store.TryGetChallenge(identity!.UsernameHashHex, out UserDataChallengeRecord? pending) || pending is null)
        {
            return UserDataStatusCode.InvalidChallenge;
        }

        if (pending.ExpiresAtUtc < _time.GetUtcNow())
        {
            _store.ClearChallenge(identity.UsernameHashHex);
            return UserDataStatusCode.ExpiredChallenge;
        }

        string actual = Convert.ToHexString(SHA256.HashData(challengeResponsePlain)).ToLowerInvariant();
        return string.Equals(actual, pending.ChallengeHashHex, StringComparison.Ordinal)
            ? UserDataStatusCode.Ok
            : UserDataStatusCode.InvalidChallenge;
    }

    private bool TryResolveIdentity(
        string username,
        out UserDataIdentityRecord? identity,
        out UserDataStatusCode status)
    {
        string hash = UserDataUsernameHash.HashHex(username);
        if (_store.TryGetIdentity(hash, out identity) && identity is not null)
        {
            status = UserDataStatusCode.Ok;
            return true;
        }

        identity = null;
        status = UserDataStatusCode.UserNotFound;
        return false;
    }
}
