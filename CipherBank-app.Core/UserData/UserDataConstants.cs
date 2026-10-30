// <copyright file="UserDataConstants.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Format and algorithm constants for cipherbank-userdata-pack-v1.</summary>
internal static class UserDataConstants
{
    internal const string PackFormat = "cipherbank-userdata-pack-v1";

    internal const string KdfFamily = "cipherbank-userdata-v1";

    internal const string KekInfoLabel = "cipherbank-userdata-v1/kek";

    internal const string EnrollSeedInfoLabel = "cipherbank-userdata-v1/enroll-seed";

    internal const string AadPrefix = "cipherbank-userdata-v1";

    internal const string BlockAlgorithm = "AES-256-GCM";

    internal const string EnrollAlgorithmRsaOaepSha256V1 = "rsa-oaep-sha256-v1";

    internal const string SymmetricAlgorithmAesGcmV1 = "AES-256-GCM";

    internal const string SuiteRsaAesGcmV1 = "userdata-rsa-aesgcm-v1";

    /// <summary>Reserved suite id for a future PQ enroll algorithm (not implemented).</summary>
    internal const string SuitePqAesGcmV1 = "userdata-pq-aesgcm-v1";

    internal const int KekLength = 32;

    internal const int EnrollSeedLength = 64;

    internal const int RsaKeySizeBits = 2048;

    internal const int Bip39SeedLength = 64;

    /// <summary>BIP39 seed PBKDF2 iteration count (spec-mandated; not a tunable hardening knob).</summary>
    internal const int Bip39SeedIterations = 2048;

    internal const int NonceSize = 12;

    internal const int TagSize = 16;

    internal const int UsernameHashPrefixLength = 8;
}
