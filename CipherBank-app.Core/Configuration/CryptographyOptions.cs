// <copyright file="CryptographyOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Non-secret algorithm parameters for custody blob encryption.</summary>
public sealed class CryptographyOptions
{
    public const string SectionName = "Cryptography";

    /// <summary>Default values compatible with the existing custody blob format.</summary>
    public static CryptographyOptions Default => new();

    public int NonceSizeBytes { get; set; } = 12;

    public int TagSizeBytes { get; set; } = 16;

    public int KeySizeBytes { get; set; } = 32;

    public int SaltSizeBytes { get; set; } = 16;

    public int Pbkdf2Iterations { get; set; } = 210_000;

    /// <summary>Returns whether values are safe and compatible with AES-GCM/SHA-256.</summary>
    public bool IsValid()
        => NonceSizeBytes == 12
            && TagSizeBytes is >= 12 and <= 16
            && KeySizeBytes is 16 or 24 or 32
            && SaltSizeBytes >= 16
            && Pbkdf2Iterations >= 210_000;
}
