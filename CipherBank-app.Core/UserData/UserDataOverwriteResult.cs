// <copyright file="UserDataOverwriteResult.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Result of OVERWRITE_USER_DATA (optional previous blob).</summary>
public sealed class UserDataOverwriteResult
{
    public UserDataOverwriteResult(UserDataStatusCode code, string? oldUserDataBlobBase64, string? details = null)
    {
        Code = code;
        OldUserDataBlobBase64 = oldUserDataBlobBase64;
        Details = details;
    }

    public UserDataStatusCode Code { get; }

    public string? OldUserDataBlobBase64 { get; }

    public string? Details { get; }

    public bool IsSuccess => Code == UserDataStatusCode.Ok;
}
