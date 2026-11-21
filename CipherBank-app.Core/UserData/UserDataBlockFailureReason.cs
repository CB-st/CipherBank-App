// <copyright file="UserDataBlockFailureReason.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Why a pack block failed to open. Use: High (GRAB apply). Scope: userdata restore.</summary>
public enum UserDataBlockFailureReason
{
    /// <summary>AES-GCM authentication failed (tampered, torn or wrong key).</summary>
    AuthenticationFailed,

    /// <summary>
    /// The block entry was null, its id or type was missing, null, empty, or whitespace-only,
    /// or its nonce, tag, or ciphertext was null or invalid Base64.
    /// </summary>
    Malformed,
}
