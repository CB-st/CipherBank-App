// <copyright file="UserDataEnrollResult.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Result of ENROLL_USER.</summary>
public sealed class UserDataEnrollResult
{
    public UserDataEnrollResult(UserDataStatusCode code, string? details = null)
    {
        Code = code;
        Details = details;
    }

    public UserDataStatusCode Code { get; }

    public string? Details { get; }

    public bool IsSuccess => Code is UserDataStatusCode.Ok or UserDataStatusCode.UsernameExists;
}
