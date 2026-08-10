// <copyright file="PaymentUri.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Wallets;

/// <summary>Payment / receive URI builder (Cora paymentUri.ts).</summary>
public static class PaymentUri
{
    private const int DefaultShortenHeadLength = 8;
    private const int DefaultShortenTailLength = 6;

    private static readonly Dictionary<string, string> SimpleSchemePrefixes = new()
    {
        ["BTC"] = "bitcoin",
        ["LTC"] = "litecoin",
        ["DOGE"] = "dogecoin",
    };

    private static readonly HashSet<string> FiatCurrencies = ["USD", "EUR", "JPY"];

    public static string Build(string symbol, string address)
        => Build(symbol, address, null, null, null);

    public static string Build(string symbol, string address, string? amount)
        => Build(symbol, address, amount, null, null);

    public static string Build(string symbol, string address, string? amount, string? label)
        => Build(symbol, address, amount, label, null);

    public static string Build(string symbol, string address, string? amount, string? label, string? message)
    {
        string sym = symbol.ToUpperInvariant();
        string addr = address.Trim();
        if (string.IsNullOrEmpty(addr))
        {
            return string.Empty;
        }

        string suffix = BuildQuerySuffix(sym, amount, label, message);
        return MapSchemeUri(sym, addr, suffix, amount).OriginalString;
    }

    public static string Shorten(string address)
        => Shorten(address, DefaultShortenHeadLength, DefaultShortenTailLength);

    public static string Shorten(string address, int head)
        => Shorten(address, head, DefaultShortenTailLength);

    public static string Shorten(string address, int head, int tail)
    {
        string a = address.Trim();
        if (a.Length <= head + tail + 1)
        {
            return a;
        }

        return string.Concat(a.AsSpan(0, head), "…", a.AsSpan(a.Length - tail));
    }

    private static string BuildQuerySuffix(string sym, string? amount, string? label, string? message)
    {
        if (sym is "ETH" or "XMR")
        {
            return string.Empty;
        }

        List<string> parts = [];
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

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    /// <summary>
    /// Dispatches a symbol to its scheme-specific URI builder (simple prefix, account-based, or fiat).
    /// Use: High (every receive-address render). Scope: PaymentUri.Build.
    /// </summary>
    private static Uri MapSchemeUri(string sym, string addr, string suffix, string? amount)
    {
        if (SimpleSchemePrefixes.TryGetValue(sym, out string? prefix))
        {
            return new Uri($"{prefix}:{addr}{suffix}", UriKind.Absolute);
        }

        if (sym == "ETH")
        {
            return BuildAccountUri("ethereum", "value", addr, amount);
        }

        if (sym == "XMR")
        {
            return BuildAccountUri("monero", "tx_amount", addr, amount);
        }

        if (FiatCurrencies.Contains(sym))
        {
            return new Uri($"cipherbank:receive/{sym}?address={Uri.EscapeDataString(addr)}", UriKind.Absolute);
        }

        return Uri.TryCreate(addr, UriKind.Absolute, out Uri? parsed)
            ? parsed
            : new Uri(addr, UriKind.Relative);
    }

    /// <summary>
    /// Formats an account-model (non-UTXO) receive URI, appending the amount query param when present.
    /// Use: High (every receive-address render for ETH/XMR). Scope: PaymentUri.MapSchemeUri.
    /// </summary>
    private static Uri BuildAccountUri(string scheme, string amountParam, string addr, string? amount)
    {
        string uriString = string.IsNullOrEmpty(amount)
            ? $"{scheme}:{addr}"
            : $"{scheme}:{addr}?{amountParam}={Uri.EscapeDataString(amount)}";
        return new Uri(uriString, UriKind.Absolute);
    }
}
