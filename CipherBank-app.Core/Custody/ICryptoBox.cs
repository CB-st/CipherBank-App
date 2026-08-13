// <copyright file="ICryptoBox.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Encrypts and decrypts version-compatible custody blobs.</summary>
public interface ICryptoBox
{
    byte[] DeriveKey(string pin, byte[] salt);

    string Seal(string plaintext, string pin);

    string Open(string sealedB64, string pin);
}
