// <copyright file="UserDataConstants.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Format and algorithm constants for cipherbank-userdata-pack-v1.</summary>
public static class UserDataConstants
{
    public const string PackFormat = "cipherbank-userdata-pack-v1";

    public const string KdfFamily = "cipherbank-userdata-v1";

    public const string KekInfoLabel = "cipherbank-userdata-v1/kek";

    public const string EnrollSeedInfoLabel = "cipherbank-userdata-v1/enroll-seed";

    public const string AadPrefix = "cipherbank-userdata-v1";

    public const string BlockAlgorithm = "AES-256-GCM";

    public const string EnrollAlgorithmRsaOaepSha256V1 = "rsa-oaep-sha256-v1";

    public const string SymmetricAlgorithmAesGcmV1 = "AES-256-GCM";

    public const string SuiteRsaAesGcmV1 = "userdata-rsa-aesgcm-v1";

    /// <summary>Reserved suite id for a future PQ enroll algorithm (not implemented).</summary>
    public const string SuitePqAesGcmV1 = "userdata-pq-aesgcm-v1";

    public const int KekLength = 32;

    public const int EnrollSeedLength = 64;

    public const int RsaKeySizeBits = 2048;

    public const int Bip39SeedLength = 64;

    public const int Bip39SeedIterations = 2048;

    public const int NonceSize = 12;

    public const int TagSize = 16;

    public const int UsernameHashPrefixLength = 8;
}
