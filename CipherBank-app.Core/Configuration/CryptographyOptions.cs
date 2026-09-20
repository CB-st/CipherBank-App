// <copyright file="CryptographyOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Non-secret algorithm parameters for custody blob encryption.</summary>
public sealed class CryptographyOptions : IOptionsSection
{
    /// <summary>AES-GCM nonce size required by the existing custody blob format.</summary>
    public const int AesGcmNonceSizeBytes = 12;

    /// <summary>Minimum authentication tag size accepted for AES-GCM.</summary>
    public const int MinTagSizeBytes = 12;

    /// <summary>Maximum authentication tag size accepted for AES-GCM.</summary>
    public const int MaxTagSizeBytes = 16;

    /// <summary>AES-128 key length in bytes.</summary>
    public const int Aes128KeySizeBytes = 16;

    /// <summary>AES-192 key length in bytes.</summary>
    public const int Aes192KeySizeBytes = 24;

    /// <summary>AES-256 key length in bytes.</summary>
    public const int Aes256KeySizeBytes = 32;

    /// <summary>Minimum PBKDF2 salt size compatible with the custody blob format.</summary>
    public const int MinSaltSizeBytes = 16;

    /// <summary>Minimum PBKDF2 iteration count compatible with the custody blob format.</summary>
    public const int MinPbkdf2Iterations = 210_000;

    /// <inheritdoc />
    public static string SectionName => "Cryptography";

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

    /// <summary>
    /// True when values match the single persisted custody blob profile.
    /// Overrides that pass <see cref="IsValid"/> still orphan existing vaults.
    /// </summary>
    public bool MatchesPersistedProfile()
    {
        CryptographyOptions persisted = Default;
        return NonceSizeBytes == persisted.NonceSizeBytes
            && TagSizeBytes == persisted.TagSizeBytes
            && KeySizeBytes == persisted.KeySizeBytes
            && SaltSizeBytes == persisted.SaltSizeBytes
            && Pbkdf2Iterations == persisted.Pbkdf2Iterations;
    }
}
