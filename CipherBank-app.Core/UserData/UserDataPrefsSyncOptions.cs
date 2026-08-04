// <copyright file="UserDataPrefsSyncOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Migration knobs for pack-backed prefs sync (dual-write window).</summary>
public sealed class UserDataPrefsSyncOptions
{
    /// <summary>When true, also PUT plaintext product prefs during the dual-write window.</summary>
    public bool DualWriteProductPrefs { get; init; } = true;

    /// <summary>
    /// Dual-write product PutPrefs while SuccessfulPackWrites is at most this value
    /// (including the write that reaches the limit). Stop on the next pack success.
    /// 0 = never stop while DualWriteProductPrefs is true.
    /// </summary>
    public int DisableProductPushAfterSuccessfulPackWrites { get; init; } = 3;

    /// <summary>When false, pack path is skipped and only product Get/PutPrefs is used.</summary>
    public bool EnablePackSync { get; init; } = true;

    /// <summary>Factory for tests / pack-only builds. Use: Low. Scope: DI.</summary>
    public static UserDataPrefsSyncOptions PackOnly()
        => new()
        {
            DualWriteProductPrefs = false,
            EnablePackSync = true,
            DisableProductPushAfterSuccessfulPackWrites = 0,
        };

    /// <summary>Default dual-write migration window. Use: Medium. Scope: DI.</summary>
    public static UserDataPrefsSyncOptions DualWrite()
        => new();
}
