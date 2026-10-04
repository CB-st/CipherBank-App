// <copyright file="PaymentUri.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Wallets;

/// <summary>Payment / receive URI builder. Chain-specific formatting lives on <see cref="IWalletModule"/>.</summary>
public static class PaymentUri
{
    private const int DefaultShortenHeadLength = 8;
    private const int DefaultShortenTailLength = 6;

    private static readonly HashSet<string> FiatCurrencies = ["USD", "EUR", "JPY"];

    /// <summary>Builds a receive URI. Empty addresses return an empty string.</summary>
    public static string Build(
        string symbol,
        string address,
        string? amount = null,
        string? label = null,
        string? message = null)
    {
        string addr = address.Trim();
        if (string.IsNullOrEmpty(addr) || !AssetSymbol.TryParse(symbol, out AssetSymbol? parsed))
        {
            return string.Empty;
        }

        if (FiatCurrencies.Contains(parsed.Value))
        {
            return new Uri(
                $"cipherbank:receive/{parsed.Value}?address={Uri.EscapeDataString(addr)}",
                UriKind.Absolute).OriginalString;
        }

        return WalletRegistry.Get(parsed).BuildReceiveUri(addr, amount, label, message).OriginalString;
    }

    /// <summary>Shortens an address for display. Lengths are presentation defaults, not deployment settings.</summary>
    public static string Shorten(string address)
        => Shorten(address, DefaultShortenHeadLength, DefaultShortenTailLength);

    /// <summary>Shortens an address, keeping <paramref name="head"/> characters and the default tail.</summary>
    public static string Shorten(string address, int head)
        => Shorten(address, head, DefaultShortenTailLength);

    /// <summary>Shortens an address to <paramref name="head"/> and <paramref name="tail"/> characters.</summary>
    public static string Shorten(string address, int head, int tail)
    {
        string trimmed = address.Trim();
        if (trimmed.Length <= head + tail + 1)
        {
            return trimmed;
        }

        return string.Concat(trimmed.AsSpan(0, head), "…", trimmed.AsSpan(trimmed.Length - tail));
    }

    /// <summary>BIP21-style URI with optional amount, label, and message query fields.</summary>
    internal static Uri Bip21(string scheme, string address, string? amount, string? label, string? message)
        => new($"{scheme}:{address}{Query(amount, label, message)}", UriKind.Absolute);

    /// <summary>Account-model URI. Amount uses <paramref name="amountParameter"/> when present.</summary>
    internal static Uri Account(string scheme, string amountParameter, string address, string? amount)
    {
        string uriString = string.IsNullOrEmpty(amount)
            ? $"{scheme}:{address}"
            : $"{scheme}:{address}?{amountParameter}={Uri.EscapeDataString(amount)}";
        return new Uri(uriString, UriKind.Absolute);
    }

    private static string Query(string? amount, string? label, string? message)
    {
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

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }
}
