// <copyright file="CryptographyOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Non-secret algorithm parameters for custody blob encryption.</summary>
public sealed record CryptographyOptions : CipherBankOptions<CryptographyOptions>
{
    /// <summary>AES-GCM nonce size required by the existing custody blob format.</summary>
    public static int AesGcmNonceSizeBytes => 12;

    /// <summary>Minimum authentication tag size accepted for AES-GCM.</summary>
    public static int MinTagSizeBytes => 12;

    /// <summary>Maximum authentication tag size accepted for AES-GCM.</summary>
    public static int MaxTagSizeBytes => 16;

    /// <summary>AES-128 key length in bytes.</summary>
    public static int Aes128KeySizeBytes => 16;

    /// <summary>AES-192 key length in bytes.</summary>
    public static int Aes192KeySizeBytes => 24;

    /// <summary>AES-256 key length in bytes.</summary>
    public static int Aes256KeySizeBytes => 32;

    /// <summary>Minimum PBKDF2 salt size compatible with the custody blob format.</summary>
    public static int MinSaltSizeBytes => 16;

    /// <summary>Minimum PBKDF2 iteration count compatible with the custody blob format.</summary>
    public static int MinPbkdf2Iterations => 210_000;

    /// <summary>Default values compatible with the existing custody blob format.</summary>
    public static CryptographyOptions Default => new();

    public int NonceSizeBytes { get; set; } = AesGcmNonceSizeBytes;

    public int TagSizeBytes { get; set; } = MaxTagSizeBytes;

    public int KeySizeBytes { get; set; } = Aes256KeySizeBytes;

    public int SaltSizeBytes { get; set; } = MinSaltSizeBytes;

    public int Pbkdf2Iterations { get; set; } = MinPbkdf2Iterations;

    /// <summary>Returns whether values are safe and compatible with AES-GCM/SHA-256.</summary>
    public bool IsValid()
        => HasValidNonceAndTag() && HasSupportedKeySize() && HasValidKdfParameters();

    /// <summary>
    /// True when values match the single persisted custody blob profile.
    /// Overrides that pass <see cref="IsValid"/> still orphan existing vaults.
    /// </summary>
    public bool MatchesPersistedProfile()
    {
        CryptographyOptions persisted = Default;
        return NonceAndTagMatch(persisted) && KeyAndKdfMatch(persisted);
    }

    private bool HasValidNonceAndTag()
        => NonceSizeBytes == AesGcmNonceSizeBytes
        && TagSizeBytes >= MinTagSizeBytes
        && TagSizeBytes <= MaxTagSizeBytes;

    private bool HasSupportedKeySize()
        => KeySizeBytes == Aes128KeySizeBytes
        || KeySizeBytes == Aes192KeySizeBytes
        || KeySizeBytes == Aes256KeySizeBytes;

    private bool HasValidKdfParameters()
        => SaltSizeBytes >= MinSaltSizeBytes
        && Pbkdf2Iterations >= MinPbkdf2Iterations;

    private bool NonceAndTagMatch(CryptographyOptions persisted)
        => NonceSizeBytes == persisted.NonceSizeBytes
        && TagSizeBytes == persisted.TagSizeBytes;

    private bool KeyAndKdfMatch(CryptographyOptions persisted)
        => KeySizeBytes == persisted.KeySizeBytes
        && SaltSizeBytes == persisted.SaltSizeBytes
        && Pbkdf2Iterations == persisted.Pbkdf2Iterations;
}
