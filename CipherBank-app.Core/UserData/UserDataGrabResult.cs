// <copyright file="UserDataGrabResult.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Result of GRAB_USER_DATA (opaque Base64 USER_DATA_BLOB).</summary>
public sealed class UserDataGrabResult
{
    public UserDataGrabResult(UserDataStatusCode code, string? userDataBlobBase64, string? details = null)
    {
        Code = code;
        UserDataBlobBase64 = userDataBlobBase64;
        Details = details;
    }

    public UserDataStatusCode Code { get; }

    public string? UserDataBlobBase64 { get; }

    public string? Details { get; }

    public bool IsSuccess => Code == UserDataStatusCode.Ok;
}
