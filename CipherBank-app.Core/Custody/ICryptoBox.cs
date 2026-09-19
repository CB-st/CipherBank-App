// <copyright file="ICryptoBox.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Custody;

/// <summary>Encrypts and decrypts version-compatible custody blobs.</summary>
public interface ICryptoBox
{
    /// <summary>
    /// Derives the custody key from <paramref name="pin"/> and <paramref name="salt"/> (PBKDF2 under
    /// the frozen persisted profile). Caller owns the returned buffer and must zero it after use.
    /// Use: Medium (seal / unlock). Scope: custody blobs on this device.
    /// </summary>
    byte[] DeriveKey(string pin, byte[] salt);

    /// <summary>
    /// Encrypts <paramref name="plaintext"/> under <paramref name="pin"/> and returns the Base64
    /// custody blob (salt, nonce, tag, ciphertext).
    /// Use: Low (seal / PIN change). Scope: custody blobs on this device.
    /// </summary>
    string Seal(string plaintext, string pin);

    /// <summary>
    /// Decrypts a custody blob produced by <see cref="Seal"/>. Throws
    /// <see cref="System.Security.Cryptography.CryptographicException"/> when the PIN is wrong or the
    /// blob is tampered. Use: Medium (unlock). Scope: custody blobs on this device.
    /// </summary>
    string Open(string sealedB64, string pin);
}
