// <copyright file="PaymentUri.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Wallets;

/// <summary>Payment / receive URI builder (Cora paymentUri.ts).</summary>
public static class PaymentUri
{
    public static string Build(string symbol, string address, string? amount = null, string? label = null, string? message = null)
    {
        string sym = symbol.ToUpperInvariant();
        string addr = address.Trim();
        if (string.IsNullOrEmpty(addr))
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(amount))
        {
            parts.Add("amount=" + Uri.EscapeDataString(amount));
        }

        if (!string.IsNullOrEmpty(label))
        {
            parts.Add("label=" + Uri.EscapeDataString(label));
        }

        if (!string.IsNullOrEmpty(message))
        {
            parts.Add("message=" + Uri.EscapeDataString(message));
        }

        string suffix = parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);

        return sym switch
        {
            "BTC" => $"bitcoin:{addr}{suffix}",
            "LTC" => $"litecoin:{addr}{suffix}",
            "DOGE" => $"dogecoin:{addr}{suffix}",
            "ETH" => string.IsNullOrEmpty(amount) ? $"ethereum:{addr}" : $"ethereum:{addr}?value={Uri.EscapeDataString(amount)}",
            "XMR" => string.IsNullOrEmpty(amount) ? $"monero:{addr}" : $"monero:{addr}?tx_amount={Uri.EscapeDataString(amount)}",
            "USD" or "EUR" or "JPY" => $"cipherbank:receive/{sym}?address={Uri.EscapeDataString(addr)}",
            _ => addr,
        };
    }

    public static string Shorten(string address, int head = 8, int tail = 6)
    {
        string a = address.Trim();
        if (a.Length <= head + tail + 1)
        {
            return a;
        }

        return string.Concat(a.AsSpan(0, head), "…", a.AsSpan(a.Length - tail));
    }
}
