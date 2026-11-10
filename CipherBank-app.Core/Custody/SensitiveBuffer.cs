// <copyright file="SensitiveBuffer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.Custody;

/// <summary>
/// Owns a secret byte array and zeroes it on <see cref="Dispose"/>.
/// Use with <c>using</c> so keys and plaintext are wiped when they leave scope.
/// </summary>
public sealed class SensitiveBuffer : IDisposable
{
    private readonly byte[] _bytes;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="SensitiveBuffer"/> class.</summary>
    /// <param name="bytes">Array to own. The caller must not keep another reference to it.</param>
    public SensitiveBuffer(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        _bytes = bytes;
    }

    /// <summary>Finalizes an instance of the <see cref="SensitiveBuffer"/> class.</summary>
    /// <remarks>Zeroes bytes a caller forgot to dispose.</remarks>
    ~SensitiveBuffer()
    {
        CryptographicOperations.ZeroMemory(_bytes);
    }

    /// <summary>Gets the buffer length in bytes.</summary>
    public int Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _bytes.Length;
        }
    }

    /// <summary>Gets a span over the secret bytes.</summary>
    public Span<byte> Span
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _bytes;
        }
    }

    /// <summary>Zeroes the owned bytes.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_bytes);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
