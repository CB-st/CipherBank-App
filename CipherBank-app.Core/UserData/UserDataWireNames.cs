// <copyright file="UserDataWireNames.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>SCREAMING_SNAKE field and message-type names for CIPHERBANK_INTERNAL userdata.</summary>
public static class UserDataWireNames
{
    public const string MessageType = "MESSAGE_TYPE";
    public const string TimeStamp = "TIME_STAMP";
    public const string Code = "CODE";
    public const string Message = "MESSAGE";
    public const string Payload = "PAYLOAD";
    public const string Details = "DETAILS";

    public const string Username = "USERNAME";
    public const string PublicKeyPem = "PUBLIC_KEY_PEM";
    public const string Preferred2FaMethod = "PREFERRED_2FA_METHOD";
    public const string EncryptedChallengeBlob = "ENCRYPTED_CHALLENGE_BLOB";
    public const string ExpiresAt = "EXPIRES_AT";
    public const string Effective2FaMethod = "EFFECTIVE_2FA_METHOD";
    public const string ChallengeResponseBlob = "CHALLENGE_RESPONSE_BLOB";
    public const string UserDataBlob = "USER_DATA_BLOB";
    public const string NewUserDataBlob = "NEW_USER_DATA_BLOB";
    public const string OldUserDataBlob = "OLD_USER_DATA_BLOB";
    public const string Overwrite = "OVERWRITE";
    public const string AreYouSure = "AREYOUSURE";

    public const string EnrollUserRequest = "ENROLL_USER_REQUEST";
    public const string EnrollUserResponse = "ENROLL_USER_RESPONSE";
    public const string ChallengeUserDataRequest = "CHALLENGE_USER_DATA_REQUEST";
    public const string ChallengeUserDataResponse = "CHALLENGE_USER_DATA_RESPONSE";
    public const string GrabUserDataRequest = "GRAB_USER_DATA_REQUEST";
    public const string GrabUserDataResponse = "GRAB_USER_DATA_RESPONSE";
    public const string OverwriteUserDataRequest = "OVERWRITE_USER_DATA_REQUEST";
    public const string OverwriteUserDataResponse = "OVERWRITE_USER_DATA_RESPONSE";
    public const string ErrorType = "ERROR";

    public const string TwoFaUnspecified = "UNSPECIFIED";
    public const string TwoFaEmail = "EMAIL";
    public const string TwoFaSms = "SMS";
    public const string TwoFaAuthenticator = "AUTHENTICATOR";
}
