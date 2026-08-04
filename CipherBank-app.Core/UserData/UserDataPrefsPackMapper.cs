// <copyright file="UserDataPrefsPackMapper.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;
using CipherBank_app.Persist;
using CipherBank_app.V1;

namespace CipherBank_app.UserData;

/// <summary>Maps <see cref="UserPrefs"/> to/from the userdata <c>prefs</c> block JSON.</summary>
public static class UserDataPrefsPackMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Serializes prefs as PrefsWireDto JSON for the pack block plaintext.
    /// Use: High (push). Scope: userdata prefs sync.
    /// </summary>
    public static string ToPrefsBlockJson(UserPrefs prefs)
    {
        ArgumentNullException.ThrowIfNull(prefs);
        PrefsWireDto dto = PrefsWireDto.FromUserPrefs(prefs);
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    /// <summary>
    /// Merges prefs-block JSON into a local UserPrefs model (AssetsLayout preserve rules via PrefsMerge).
    /// Use: High (pull). Scope: userdata prefs sync.
    /// </summary>
    public static void ApplyPrefsBlockJson(UserPrefs local, string prefsBlockJson)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefsBlockJson);

        PrefsWireDto? dto = JsonSerializer.Deserialize<PrefsWireDto>(prefsBlockJson, JsonOptions);
        PrefsMerge.Merge(local, dto);
    }
}
