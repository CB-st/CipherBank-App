// <copyright file="AccountBootstrap.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>
/// Returning-user bootstrap payload. Intentionally contains no mnemonic/seed/PIN fields.
/// </summary>
public sealed class AccountBootstrapDto
{
    [JsonPropertyName("PREFS")]
    public PrefsWireDto? Prefs { get; set; }

    [JsonPropertyName("prefs")]
    public PrefsWireDto? PrefsCamel { get; set; }

    [JsonPropertyName("RECIPIENTS")]
    public List<BootstrapRecipientDto>? Recipients { get; set; }

    [JsonPropertyName("recipients")]
    public List<BootstrapRecipientDto>? RecipientsCamel { get; set; }

    [JsonPropertyName("SYNCED_AT")]
    public long? SyncedAt { get; set; }

    [JsonPropertyName("syncedAt")]
    public long? SyncedAtCamel { get; set; }

    public PrefsWireDto? ResolvedPrefs => Prefs ?? PrefsCamel;

    public IReadOnlyList<BootstrapRecipientDto> ResolvedRecipients
        => (IReadOnlyList<BootstrapRecipientDto>?)(Recipients ?? RecipientsCamel) ?? Array.Empty<BootstrapRecipientDto>();
}
