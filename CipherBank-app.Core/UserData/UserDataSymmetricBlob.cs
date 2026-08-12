// <copyright file="UserDataSymmetricBlob.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Nonce/tag/ciphertext triple from <see cref="IUserDataSymmetricCipher"/>.</summary>
public sealed class UserDataSymmetricBlob
{
    public UserDataSymmetricBlob(byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(nonce);
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(ciphertext);
        Nonce = nonce;
        Tag = tag;
        Ciphertext = ciphertext;
    }

    public byte[] Nonce { get; }

    public byte[] Tag { get; }

    public byte[] Ciphertext { get; }
}
