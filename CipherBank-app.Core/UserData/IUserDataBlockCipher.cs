// <copyright file="IUserDataBlockCipher.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>
/// Pack-block AEAD with userdata AAD binding (type/id/version/username hash).
/// Built on <see cref="IUserDataSymmetricCipher"/> so AEAD can be swapped independently of enroll.
/// </summary>
public interface IUserDataBlockCipher
{
    string AlgorithmId { get; }

    /// <summary>
    /// Seals a plain block into wire form. Use: High (SealPack). Scope: pack codec.
    /// </summary>
    UserDataBlockWire Seal(
        UserDataPlainBlock plain,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion);

    /// <summary>
    /// Opens a wire block to UTF-8 plaintext. Use: High (OpenPack). Scope: pack codec.
    /// </summary>
    string Open(
        UserDataBlockWire block,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion);
}
