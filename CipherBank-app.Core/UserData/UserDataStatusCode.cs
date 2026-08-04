// <copyright file="UserDataStatusCode.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Wire error codes from CipherBank-src UserData_APIMessage.</summary>
public enum UserDataStatusCode
{
    Ok = 0,
    UnknownRequest = -1,
    UsernameExists = -2,
    UserNotFound = -3,
    InvalidPublicKey = -4,
    InvalidChallenge = -5,
    ExpiredChallenge = -6,
    OverwriteNotConfirmed = -7,
    TwoFaDenied = -8,
    InvalidTwoFaMethod = -9,
    CryptographicFailure = -10,
    DatabaseFailure = -11,
    TwoFaServiceFailure = -12,
    TransportFailure = -100,
}
