// <copyright file="UserDataPackMeta.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Local pack content_version and migration counters.</summary>
public sealed class UserDataPackMeta
{
    public uint ContentVersion { get; set; }

    public int SuccessfulPackWrites { get; set; }
}
