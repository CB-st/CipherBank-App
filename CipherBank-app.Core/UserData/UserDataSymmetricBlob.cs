// <copyright file="UserDataSymmetricBlob.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Nonce/tag/ciphertext triple from <see cref="IUserDataSymmetricCipher"/>.</summary>
/// <param name="nonce">Nonce used to seal the blob.</param>
/// <param name="tag">Authentication tag.</param>
/// <param name="ciphertext">Encrypted payload.</param>
public sealed class UserDataSymmetricBlob(byte[] nonce, byte[] tag, byte[] ciphertext)
{
    public byte[] Nonce { get; } = nonce;

    public byte[] Tag { get; } = tag;

    public byte[] Ciphertext { get; } = ciphertext;
}
