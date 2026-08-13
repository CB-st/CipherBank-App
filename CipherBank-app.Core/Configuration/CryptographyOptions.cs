// <copyright file="CryptographyOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Non-secret algorithm parameters for custody blob encryption.</summary>
public sealed class CryptographyOptions
{
    public static string SectionName { get; } = "Cryptography";

    /// <summary>AES-GCM nonce size required by the existing custody blob format.</summary>
    public static int AesGcmNonceSizeBytes { get; } = 12;

    /// <summary>Minimum authentication tag size accepted for AES-GCM.</summary>
    public static int MinTagSizeBytes { get; } = 12;

    /// <summary>Maximum authentication tag size accepted for AES-GCM.</summary>
    public static int MaxTagSizeBytes { get; } = 16;

    /// <summary>AES-128 key length in bytes.</summary>
    public static int Aes128KeySizeBytes { get; } = 16;

    /// <summary>AES-192 key length in bytes.</summary>
    public static int Aes192KeySizeBytes { get; } = 24;

    /// <summary>AES-256 key length in bytes.</summary>
    public static int Aes256KeySizeBytes { get; } = 32;

    /// <summary>Minimum PBKDF2 salt size compatible with the custody blob format.</summary>
    public static int MinSaltSizeBytes { get; } = 16;

    /// <summary>Minimum PBKDF2 iteration count compatible with the custody blob format.</summary>
    public static int MinPbkdf2Iterations { get; } = 210_000;

    /// <summary>Default values compatible with the existing custody blob format.</summary>
    public static CryptographyOptions Default => new();

    public int NonceSizeBytes { get; set; } = AesGcmNonceSizeBytes;

    public int TagSizeBytes { get; set; } = MaxTagSizeBytes;

    public int KeySizeBytes { get; set; } = Aes256KeySizeBytes;

    public int SaltSizeBytes { get; set; } = MinSaltSizeBytes;

    public int Pbkdf2Iterations { get; set; } = MinPbkdf2Iterations;

    /// <summary>Returns whether values are safe and compatible with AES-GCM/SHA-256.</summary>
    public bool IsValid()
    {
        if (NonceSizeBytes != AesGcmNonceSizeBytes)
        {
            return false;
        }

        if (TagSizeBytes < MinTagSizeBytes || TagSizeBytes > MaxTagSizeBytes)
        {
            return false;
        }

        if (KeySizeBytes != Aes128KeySizeBytes
            && KeySizeBytes != Aes192KeySizeBytes
            && KeySizeBytes != Aes256KeySizeBytes)
        {
            return false;
        }

        if (SaltSizeBytes < MinSaltSizeBytes)
        {
            return false;
        }

        return Pbkdf2Iterations >= MinPbkdf2Iterations;
    }
}
