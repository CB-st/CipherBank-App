// <copyright file="CryptographyOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Non-secret algorithm parameters for custody blob encryption.</summary>
public sealed record CryptographyOptions : CipherBankOptions<CryptographyOptions>
{
    private const int DefaultNonceBytes = 12;
    private const int MinimumTagBytes = 12;
    private const int MaximumTagBytes = 16;
    private const int Aes128KeyBytes = 16;
    private const int Aes192KeyBytes = 24;
    private const int Aes256KeyBytes = 32;
    private const int MinimumSaltBytes = 16;
    private const int MinimumPbkdf2Iterations = 210_000;
    private const byte CurrentBlobFormatVersion = 0x01;

    /// <summary>AES-GCM nonce size required by the existing custody blob format.</summary>
    public static int AesGcmNonceSizeBytes => DefaultNonceBytes;

    /// <summary>Minimum authentication tag size accepted for AES-GCM.</summary>
    public static int MinTagSizeBytes => MinimumTagBytes;

    /// <summary>Maximum authentication tag size accepted for AES-GCM.</summary>
    public static int MaxTagSizeBytes => MaximumTagBytes;

    /// <summary>AES-128 key length in bytes.</summary>
    public static int Aes128KeySizeBytes => Aes128KeyBytes;

    /// <summary>AES-192 key length in bytes.</summary>
    public static int Aes192KeySizeBytes => Aes192KeyBytes;

    /// <summary>AES-256 key length in bytes.</summary>
    public static int Aes256KeySizeBytes => Aes256KeyBytes;

    /// <summary>Minimum PBKDF2 salt size compatible with the custody blob format.</summary>
    public static int MinSaltSizeBytes => MinimumSaltBytes;

    /// <summary>Minimum PBKDF2 iteration count compatible with the custody blob format.</summary>
    public static int MinPbkdf2Iterations => MinimumPbkdf2Iterations;

    /// <summary>Default values compatible with the existing custody blob format.</summary>
    public static CryptographyOptions Default => new();

    public int NonceSizeBytes { get; set; } = AesGcmNonceSizeBytes;

    public int TagSizeBytes { get; set; } = MaxTagSizeBytes;

    public int KeySizeBytes { get; set; } = Aes256KeySizeBytes;

    public int SaltSizeBytes { get; set; } = MinSaltSizeBytes;

    public int Pbkdf2Iterations { get; set; } = MinPbkdf2Iterations;

    /// <summary>
    /// Gets or sets the leading envelope byte <see cref="Custody.AesGcmCryptoBox"/> writes.
    /// Part of the frozen persisted profile: <see cref="MatchesPersistedProfile"/> rejects any other value.
    /// </summary>
    public byte BlobFormatVersion { get; set; } = CurrentBlobFormatVersion;

    /// <summary>Returns whether values are safe and compatible with AES-GCM/SHA-256.</summary>
    public bool IsValid()
        => (NonceSizeBytes, TagSizeBytes, KeySizeBytes, SaltSizeBytes, Pbkdf2Iterations) is
        (
            DefaultNonceBytes, // AES-GCM nonce: fixed 12 bytes.
            >= MinimumTagBytes and <= MaximumTagBytes, // AES-GCM tag: 12 to 16 bytes.
            Aes128KeyBytes or Aes192KeyBytes or Aes256KeyBytes, // AES-128/192/256 key.
            >= MinimumSaltBytes, // PBKDF2 salt floor.
            >= MinimumPbkdf2Iterations); // PBKDF2 iteration floor.

    /// <summary>
    /// True when values match the single persisted custody blob profile.
    /// Overrides that pass <see cref="IsValid"/> still orphan existing vaults.
    /// </summary>
    public bool MatchesPersistedProfile()
        => this == Default;
}
