// <copyright file="WireModels.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Portfolio snapshot from GET /portfolio.</summary>
public sealed class PortfolioDto
{
    [JsonPropertyName("TOTAL_USD")]
    public string TotalUsd { get; set; } = "0";

    [JsonPropertyName("CHANGE_24H_PCT")]
    public string Change24HPct { get; set; } = "0";

    [JsonPropertyName("HOLDINGS")]
    public List<HoldingDto> Holdings { get; set; } = new();
}

/// <summary>Single holding line.</summary>
public sealed class HoldingDto
{
    [JsonPropertyName("SYMBOL")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("NAME")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("BALANCE")]
    public string Balance { get; set; } = "0";

    [JsonPropertyName("USD_VALUE")]
    public string UsdValue { get; set; } = "0";

    [JsonPropertyName("CHANGE_24H_PCT")]
    public string Change24HPct { get; set; } = "0";
}

/// <summary>OHLC / history point.</summary>
public sealed class HistoryPointDto
{
    [JsonPropertyName("T")]
    public long T { get; set; }

    [JsonPropertyName("V")]
    public double V { get; set; }
}

/// <summary>Quote lock response.</summary>
public sealed class QuoteDto
{
    [JsonPropertyName("FROM")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("TO")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("RATE")]
    public string Rate { get; set; } = "0";

    [JsonPropertyName("EXPIRES_AT")]
    public long ExpiresAt { get; set; }
}

/// <summary>Session tokens.</summary>
public sealed class SessionDto
{
    [JsonPropertyName("ACCESS_TOKEN")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("REFRESH_TOKEN")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("EXPIRES_AT")]
    public long ExpiresAt { get; set; }
}

/// <summary>Receive address payload.</summary>
public sealed class ReceiveDto
{
    [JsonPropertyName("ASSET")]
    public string Asset { get; set; } = string.Empty;

    [JsonPropertyName("ADDRESS")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("URI")]
    public string? Uri { get; set; }
}

/// <summary>POS session.</summary>
public sealed class PosSessionDto
{
    [JsonPropertyName("SESSION_ID")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("STATUS")]
    public string Status { get; set; } = "pending_auth";

    [JsonPropertyName("TOKEN_REF")]
    public string? TokenRef { get; set; }

    [JsonPropertyName("LAST4")]
    public string? Last4 { get; set; }

    [JsonPropertyName("BRAND")]
    public string? Brand { get; set; }

    [JsonPropertyName("TTL_MS")]
    public long? TtlMs { get; set; }
}

/// <summary>Vault card metadata (never PAN).</summary>
public sealed class VaultCardDto
{
    [JsonPropertyName("CARD_ID")]
    public string CardId { get; set; } = string.Empty;

    [JsonPropertyName("LAST4")]
    public string Last4 { get; set; } = "4242";

    [JsonPropertyName("BRAND")]
    public string Brand { get; set; } = "visa";

    [JsonPropertyName("LABEL")]
    public string Label { get; set; } = "Hardware test";

    [JsonPropertyName("HARDWARE_TEST")]
    public bool HardwareTest { get; set; } = true;
}

/// <summary>Money movement ack.</summary>
public sealed class MoneyMoveDto
{
    [JsonPropertyName("ID")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("STATUS")]
    public string Status { get; set; } = "pending";
}


/// <summary>Server-held vault binary metadata (never mnemonic).</summary>
public sealed class VaultBinaryDto
{
    [JsonPropertyName("BINARY_ID")]
    public string BinaryId { get; set; } = string.Empty;

    [JsonPropertyName("LABEL")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("KIND")]
    public string Kind { get; set; } = "wallet_rpc";
}
