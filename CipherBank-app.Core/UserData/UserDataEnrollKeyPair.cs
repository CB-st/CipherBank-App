// <copyright file="UserDataEnrollKeyPair.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.UserData;

/// <summary>
/// Rematerialized enroll keypair (public PEM + PKCS#8 private DER). Dispose zeros private bytes.
/// </summary>
public sealed class UserDataEnrollKeyPair : IDisposable
{
    private byte[]? _privateKeyPkcs8Der;
    private bool _disposed;

    /// <summary>
    /// Wraps enroll algorithm output for challenge decrypt / ENROLL_USER.
    /// Use: Medium (unlock). Scope: userdata enroll session.
    /// </summary>
    public UserDataEnrollKeyPair(
        string algorithmId,
        string publicKeyPem,
        string spkiFingerprintSha256Hex,
        byte[] privateKeyPkcs8Der)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(algorithmId);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyPem);
        ArgumentException.ThrowIfNullOrWhiteSpace(spkiFingerprintSha256Hex);
        ArgumentNullException.ThrowIfNull(privateKeyPkcs8Der);
        if (privateKeyPkcs8Der.Length == 0)
        {
            throw new ArgumentException("Private key DER is empty.", nameof(privateKeyPkcs8Der));
        }

        AlgorithmId = algorithmId;
        PublicKeyPem = publicKeyPem;
        SpkiFingerprintSha256Hex = spkiFingerprintSha256Hex;
        _privateKeyPkcs8Der = privateKeyPkcs8Der;
    }

    public string AlgorithmId { get; }

    public string PublicKeyPem { get; }

    public string SpkiFingerprintSha256Hex { get; }

    /// <summary>
    /// PKCS#8 private key DER (zeros on dispose). Use: High (DecryptChallenge). Scope: enroll algorithm.
    /// </summary>
    public ReadOnlySpan<byte> PrivateKeyPkcs8Der
    {
        get
        {
            ThrowIfDisposed();
            return _privateKeyPkcs8Der;
        }
    }

    /// <summary>
    /// Zeros private key material. Use: High (lock). Scope: session wipe.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_privateKeyPkcs8Der is not null)
        {
            CryptographicOperations.ZeroMemory(_privateKeyPkcs8Der);
            _privateKeyPkcs8Der = null;
        }

        _disposed = true;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
