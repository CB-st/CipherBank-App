// <copyright file="UserDataIdentityRecord.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Company-readable identity held by in-memory / loopback userdata stores.</summary>
public sealed class UserDataIdentityRecord
{
    public UserDataIdentityRecord(string usernameNormalized, string usernameHashHex, string publicKeyPem)
    {
        UsernameNormalized = usernameNormalized;
        UsernameHashHex = usernameHashHex;
        PublicKeyPem = publicKeyPem;
    }

    public string UsernameNormalized { get; }

    public string UsernameHashHex { get; }

    public string PublicKeyPem { get; }
}
