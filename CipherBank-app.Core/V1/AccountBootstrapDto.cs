// <copyright file="AccountBootstrapDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>
/// Returning-user bootstrap payload. Intentionally contains no mnemonic/seed/PIN fields.
/// SCREAMING_SNAKE on write; camelCase accepted on read via ExtensionData.
/// </summary>
public sealed class AccountBootstrapDto
{
    [JsonPropertyName("PREFS")]
    public PrefsWireDto? Prefs { get; set; }

    [JsonPropertyName("RECIPIENTS")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<BootstrapRecipientDto> Recipients { get; } = [];

    [JsonPropertyName("SYNCED_AT")]
    public long? SyncedAt { get; set; }

    /// <summary>Captures camelCase aliases for fold-in after deserialize.</summary>
    [JsonExtensionData]
    [JsonInclude]
    public Dictionary<string, JsonElement>? ExtensionData { get; private set; }

    public PrefsWireDto? ResolvedPrefs
    {
        get
        {
            FoldAlternateNames();
            return Prefs;
        }
    }

    public IReadOnlyList<BootstrapRecipientDto> ResolvedRecipients
    {
        get
        {
            FoldAlternateNames();
            return Recipients;
        }
    }

    /// <summary>
    /// Folds camelCase extension keys into primary properties once.
    /// Use: High (bootstrap deserialize). Scope: this DTO.
    /// </summary>
    public void FoldAlternateNames()
    {
        Dictionary<string, JsonElement>? data = ExtensionData;
        if (data is null || data.Count == 0)
        {
            FoldNested();
            return;
        }

        if (Prefs is null && WireJson.TryDeserializeObject(data, "prefs", out PrefsWireDto? prefs))
        {
            Prefs = prefs;
        }

        if (Recipients.Count == 0
            && WireJson.TryDeserializeObjectList(data, "recipients", out List<BootstrapRecipientDto>? recipients)
            && recipients is not null)
        {
            foreach (BootstrapRecipientDto recipient in recipients)
            {
                Recipients.Add(recipient);
            }
        }

        SyncedAt ??= WireJson.TryGetInt64(data, "syncedAt");
        ExtensionData = null;
        FoldNested();
    }

    /// <summary>
    /// Folds nested prefs/recipients when primary keys were already bound.
    /// Use: High (FoldAlternateNames). Scope: this DTO.
    /// </summary>
    private void FoldNested()
    {
        Prefs?.FoldAlternateNames();
        foreach (BootstrapRecipientDto recipient in Recipients)
        {
            recipient.FoldAlternateNames();
        }
    }
}
