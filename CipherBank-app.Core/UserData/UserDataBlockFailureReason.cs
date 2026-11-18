// <copyright file="UserDataBlockFailureReason.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Why a pack block failed to open. Use: High (GRAB apply). Scope: userdata restore.</summary>
public enum UserDataBlockFailureReason
{
    /// <summary>AES-GCM authentication failed (tampered, torn or wrong key).</summary>
    AuthenticationFailed,

    /// <summary>Nonce, tag or ciphertext was not valid Base64.</summary>
    Malformed,
}
