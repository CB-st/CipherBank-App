// <copyright file="CurrencySymbolMap.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Services;

/// <summary>
/// Maps app ticker symbols to CipherBank public API currency codes and back.
/// </summary>
public static class CurrencySymbolMap
{
    private const string ApiBitcoin = "BITCOIN";
    private const string ApiMonero = "MONERO";

    private static readonly Dictionary<string, string> AppToApi = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = ApiBitcoin,
        [ApiBitcoin] = ApiBitcoin,
        ["XMR"] = ApiMonero,
        [ApiMonero] = ApiMonero,
        ["USD"] = "USD",
    };

    private static readonly Dictionary<string, string> ApiToApp = new(StringComparer.OrdinalIgnoreCase)
    {
        [ApiBitcoin] = "BTC",
        [ApiMonero] = "XMR",
        ["USD"] = "USD",
    };

    /// <summary>
    /// Converts an app ticker (e.g. BTC) to a public API currency code (e.g. BITCOIN).
    /// </summary>
    /// <param name="appSymbol">App or API symbol.</param>
    /// <returns>Uppercase API currency code.</returns>
    /// <exception cref="ArgumentException">When the symbol is unsupported.</exception>
    public static string ToApiCurrency(string appSymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSymbol);

        if (AppToApi.TryGetValue(appSymbol.Trim(), out var api))
        {
            return api;
        }

        throw new ArgumentException($"Unsupported currency symbol '{appSymbol}'.", nameof(appSymbol));
    }

    /// <summary>
    /// Converts a public API currency code to an app ticker.
    /// </summary>
    /// <param name="apiCurrency">API currency code.</param>
    /// <returns>App ticker symbol.</returns>
    public static string ToAppSymbol(string apiCurrency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiCurrency);

        var key = apiCurrency.Trim().ToUpperInvariant();
        return ApiToApp.TryGetValue(key, out var app) ? app : key;
    }

    /// <summary>
    /// Returns true when the symbol can be sent to the public API.
    /// </summary>
    /// <param name="symbol">App or API symbol.</param>
    /// <returns>True when mapped.</returns>
    public static bool IsSupported(string? symbol)
    {
        return !string.IsNullOrWhiteSpace(symbol) && AppToApi.ContainsKey(symbol.Trim());
    }
}
