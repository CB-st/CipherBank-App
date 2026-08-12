// <copyright file="BootstrapRecipientDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>ACH contact from account bootstrap (no custody material).</summary>
public sealed class BootstrapRecipientDto
{
    private const int SyntheticIdHexChars = 16;

    [JsonPropertyName("ID")]
    public string? Id { get; set; }

    [JsonPropertyName("DISPLAY_NAME")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("ACCOUNT_HOLDER_NAME")]
    public string? AccountHolderName { get; set; }

    [JsonPropertyName("BANK_NAME")]
    public string? BankName { get; set; }

    [JsonPropertyName("ROUTING_NUMBER")]
    public string? RoutingNumber { get; set; }

    [JsonPropertyName("ACCOUNT_LAST4")]
    public string? AccountLast4 { get; set; }

    [JsonPropertyName("ACCOUNT_TYPE")]
    public string? AccountType { get; set; }

    [JsonPropertyName("MEMO")]
    public string? Memo { get; set; }

    /// <summary>Captures camelCase aliases for fold-in after deserialize.</summary>
    [JsonExtensionData]
    [JsonInclude]
    public Dictionary<string, JsonElement>? ExtensionData { get; private set; }

    public string ResolvedId
    {
        get
        {
            FoldAlternateNames();
            if (!string.IsNullOrWhiteSpace(Id))
            {
                return Id;
            }

            // Stable synthetic key: name alone collides when two payees share a display name.
            // Include routing + last4 so UpsertAsync keys stay distinct. Lowercase for id stability.
            string seed = string.Join(
                '|',
                ResolvedName.Trim().ToLowerInvariant(), // NOSONAR (S4040)
                (ResolvedRouting ?? string.Empty).Trim(),
                (ResolvedLast4 ?? string.Empty).Trim());

            if (string.IsNullOrWhiteSpace(seed.Replace("|", string.Empty, StringComparison.Ordinal)))
            {
                return "recipient_unknown";
            }

            return "bootstrap_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed)))
                .ToLowerInvariant()[..SyntheticIdHexChars]; // NOSONAR (S4040 — stable hex id)
        }
    }

    public string ResolvedName
    {
        get
        {
            FoldAlternateNames();
            return DisplayName ?? string.Empty;
        }
    }

    public string? ResolvedHolder
    {
        get
        {
            FoldAlternateNames();
            return AccountHolderName;
        }
    }

    public string? ResolvedBank
    {
        get
        {
            FoldAlternateNames();
            return BankName;
        }
    }

    public string? ResolvedRouting
    {
        get
        {
            FoldAlternateNames();
            return RoutingNumber;
        }
    }

    public string? ResolvedLast4
    {
        get
        {
            FoldAlternateNames();
            return AccountLast4;
        }
    }

    public string ResolvedAccountType
    {
        get
        {
            FoldAlternateNames();
            string type = (AccountType ?? "checking").ToUpperInvariant();
            return type == "SAVINGS" ? "savings" : "checking";
        }
    }

    public string? ResolvedMemo
    {
        get
        {
            FoldAlternateNames();
            return Memo;
        }
    }

    /// <summary>
    /// Folds camelCase extension keys into primary properties once.
    /// Use: High (bootstrap deserialize / Resolved*). Scope: this DTO.
    /// </summary>
    public void FoldAlternateNames()
    {
        Dictionary<string, JsonElement>? data = ExtensionData;
        if (data is null || data.Count == 0)
        {
            return;
        }

        FoldIdentityFields(data);
        FoldAccountFields(data);
        ExtensionData = null;
    }

    private void FoldIdentityFields(IDictionary<string, JsonElement> data)
    {
        Id ??= WireJson.TryGetString(data, "id");
        DisplayName ??= WireJson.TryGetString(data, "displayName");
        AccountHolderName ??= WireJson.TryGetString(data, "accountHolderName");
        BankName ??= WireJson.TryGetString(data, "bankName");
    }

    private void FoldAccountFields(IDictionary<string, JsonElement> data)
    {
        RoutingNumber ??= WireJson.TryGetString(data, "routingNumber");
        AccountLast4 ??= WireJson.TryGetString(data, "accountLast4");
        AccountType ??= WireJson.TryGetString(data, "accountType");
        Memo ??= WireJson.TryGetString(data, "memo");
    }
}
