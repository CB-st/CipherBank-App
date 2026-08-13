// <copyright file="UserDataConstants.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Format and algorithm constants for cipherbank-userdata-pack-v1.</summary>
public static class UserDataConstants
{
    public static string PackFormat { get; } = "cipherbank-userdata-pack-v1";

    public static string KdfFamily { get; } = "cipherbank-userdata-v1";

    public static string KekInfoLabel { get; } = "cipherbank-userdata-v1/kek";

    public static string EnrollSeedInfoLabel { get; } = "cipherbank-userdata-v1/enroll-seed";

    public static string AadPrefix { get; } = "cipherbank-userdata-v1";

    public static string BlockAlgorithm { get; } = "AES-256-GCM";

    public static string EnrollAlgorithmRsaOaepSha256V1 { get; } = "rsa-oaep-sha256-v1";

    public static string SymmetricAlgorithmAesGcmV1 { get; } = "AES-256-GCM";

    public static string SuiteRsaAesGcmV1 { get; } = "userdata-rsa-aesgcm-v1";

    /// <summary>Reserved suite id for a future PQ enroll algorithm (not implemented).</summary>
    public static string SuitePqAesGcmV1 { get; } = "userdata-pq-aesgcm-v1";

    public static int KekLength { get; } = 32;

    public static int EnrollSeedLength { get; } = 64;

    public static int RsaKeySizeBits { get; } = 2048;

    public static int Bip39SeedLength { get; } = 64;

    /// <summary>BIP39 seed PBKDF2 iteration count (spec-mandated; not a tunable hardening knob).</summary>
    public static int Bip39SeedIterations { get; } = 2048;

    public static int NonceSize { get; } = 12;

    public static int TagSize { get; } = 16;

    public static int UsernameHashPrefixLength { get; } = 8;
}
