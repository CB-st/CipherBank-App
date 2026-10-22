// <copyright file="PreferenceEntity.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist.Entities;

/// <summary>
/// On-device key/value row for serialized user preference JSON.
/// <see cref="CipherBank_app.Persist.PrefsStore"/> uses key <c>user_prefs</c>; <see cref="Value"/> is
/// System.Text.Json text of <see cref="CipherBank_app.Persist.UserPrefs"/> (home layout, enabled
/// currencies), not custody material or a secret.
/// </summary>
public sealed record PreferenceEntity
{
    /// <summary>Preference bag identifier (currently <c>user_prefs</c>).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>JSON payload for the key. Cleartext UI prefs, not PIN/mnemonic/key material.</summary>
    public string Value { get; set; } = string.Empty;
}
