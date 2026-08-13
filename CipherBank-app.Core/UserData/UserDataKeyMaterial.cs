// <copyright file="UserDataKeyMaterial.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.UserData;

/// <summary>
/// Holds mnemonic-derived pack KEK and enroll-seed bytes; zeros on dispose.
/// Lifetime: unlocked session only — rematerialize after PIN/biometric unlock.
/// </summary>
public sealed class UserDataKeyMaterial : IDisposable
{
    private byte[]? _kek;
    private byte[]? _enrollSeed;
    private bool _disposed;

    /// <summary>
    /// Wraps copies of KEK and enroll-seed for session-scoped use.
    /// Use: Medium (after unlock). Scope: userdata sync / pack seal.
    /// </summary>
    public UserDataKeyMaterial(ReadOnlySpan<byte> kek, ReadOnlySpan<byte> enrollSeed)
    {
        if (kek.Length != UserDataConstants.KekLength)
        {
            throw new ArgumentException($"KEK must be {UserDataConstants.KekLength} bytes.", nameof(kek));
        }

        if (enrollSeed.Length != UserDataConstants.EnrollSeedLength)
        {
            throw new ArgumentException(
                $"Enroll seed must be {UserDataConstants.EnrollSeedLength} bytes.",
                nameof(enrollSeed));
        }

        _kek = kek.ToArray();
        _enrollSeed = enrollSeed.ToArray();
    }

    /// <summary>32-byte AES-256-GCM key for data blocks. Use: High (seal/open). Scope: session.</summary>
    public ReadOnlySpan<byte> Kek
    {
        get
        {
            ThrowIfDisposed();
            return _kek;
        }
    }

    /// <summary>
    /// 64-byte seed for deterministic enroll RSA (generator lands in a follow-up PR).
    /// Use: Low until RSA ships. Scope: session.
    /// </summary>
    public ReadOnlySpan<byte> EnrollSeed
    {
        get
        {
            ThrowIfDisposed();
            return _enrollSeed;
        }
    }

    /// <summary>
    /// Zeros KEK and enroll-seed buffers.
    /// Use: High (lock / TTL wipe). Scope: AppSession lock path.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_kek is not null)
        {
            CryptographicOperations.ZeroMemory(_kek);
            _kek = null;
        }

        if (_enrollSeed is not null)
        {
            CryptographicOperations.ZeroMemory(_enrollSeed);
            _enrollSeed = null;
        }

        _disposed = true;
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed, this);
}
