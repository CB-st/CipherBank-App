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

/// <summary>ACH contact from account bootstrap (no custody material).</summary>
public sealed class BootstrapRecipientDto
{
    [JsonPropertyName("ID")]
    public string? Id { get; set; }

    [JsonPropertyName("id")]
    public string? IdCamel { get; set; }

    [JsonPropertyName("DISPLAY_NAME")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayNameCamel { get; set; }

    [JsonPropertyName("ACCOUNT_HOLDER_NAME")]
    public string? AccountHolderName { get; set; }

    [JsonPropertyName("accountHolderName")]
    public string? AccountHolderNameCamel { get; set; }

    [JsonPropertyName("BANK_NAME")]
    public string? BankName { get; set; }

    [JsonPropertyName("bankName")]
    public string? BankNameCamel { get; set; }

    [JsonPropertyName("ROUTING_NUMBER")]
    public string? RoutingNumber { get; set; }

    [JsonPropertyName("routingNumber")]
    public string? RoutingNumberCamel { get; set; }

    [JsonPropertyName("ACCOUNT_LAST4")]
    public string? AccountLast4 { get; set; }

    [JsonPropertyName("accountLast4")]
    public string? AccountLast4Camel { get; set; }

    [JsonPropertyName("ACCOUNT_TYPE")]
    public string? AccountType { get; set; }

    [JsonPropertyName("accountType")]
    public string? AccountTypeCamel { get; set; }

    [JsonPropertyName("MEMO")]
    public string? Memo { get; set; }

    [JsonPropertyName("memo")]
    public string? MemoCamel { get; set; }

    public string ResolvedId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Id))
            {
                return Id!;
            }

            if (!string.IsNullOrWhiteSpace(IdCamel))
            {
                return IdCamel!;
            }

            // Stable synthetic key so re-bootstrap does not duplicate the same payee.
            string seed = ResolvedName.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(seed))
            {
                seed = (ResolvedLast4 ?? string.Empty) + "|" + (ResolvedRouting ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(seed))
            {
                return "recipient_unknown";
            }

            return "bootstrap_" + Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(seed)))
                .ToLowerInvariant()[..16];
        }
    }

    public string ResolvedName => DisplayName ?? DisplayNameCamel ?? string.Empty;

    public string? ResolvedHolder => AccountHolderName ?? AccountHolderNameCamel;

    public string? ResolvedBank => BankName ?? BankNameCamel;

    public string? ResolvedRouting => RoutingNumber ?? RoutingNumberCamel;

    public string? ResolvedLast4 => AccountLast4 ?? AccountLast4Camel;

    public string ResolvedAccountType => (AccountType ?? AccountTypeCamel ?? "checking").ToLowerInvariant();

    public string? ResolvedMemo => Memo ?? MemoCamel;
}
