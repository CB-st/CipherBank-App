// <copyright file="CurrencySymbolMap.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Maps app ticker symbols to CipherBank public API currency codes and back.
/// App identity is <see cref="AssetSymbol"/> (open set; not an enum). Provider
/// codes such as BITCOIN stay in this map and are not a second ticker type.
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
    public static string ToApiCurrency(AssetSymbol appSymbol)
    {
        ArgumentNullException.ThrowIfNull(appSymbol);
        if (AppToApi.TryGetValue(appSymbol.Value, out string? api))
        {
            return api;
        }

        throw new ArgumentException($"Unsupported currency symbol '{appSymbol.Value}'.", nameof(appSymbol));
    }

    /// <summary>
    /// Converts ticker text to a public API currency code.
    /// </summary>
    public static string ToApiCurrency(string appSymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appSymbol);
        return ToApiCurrency(new AssetSymbol(appSymbol));
    }

    /// <summary>
    /// Converts a public API currency code to an app ticker.
    /// </summary>
    /// <param name="apiCurrency">API currency code.</param>
    /// <returns>App ticker symbol.</returns>
    public static AssetSymbol ToAppSymbol(AssetSymbol apiCurrency)
    {
        ArgumentNullException.ThrowIfNull(apiCurrency);
        string ticker = ApiToApp.TryGetValue(apiCurrency.Value, out string? app) ? app : apiCurrency.Value;
        return new AssetSymbol(ticker);
    }

    /// <summary>
    /// Converts a public API currency code to an app ticker.
    /// Unknown codes pass through uppercased so callers can still display them.
    /// </summary>
    public static string ToAppSymbol(string apiCurrency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiCurrency);
        return ToAppSymbol(new AssetSymbol(apiCurrency)).Value;
    }

    /// <summary>
    /// Returns true when the symbol can be sent to the public API.
    /// </summary>
    /// <param name="symbol">App or API symbol.</param>
    /// <returns>True when mapped.</returns>
    public static bool IsSupported(string? symbol)
        => AssetSymbol.TryParse(symbol, out AssetSymbol? parsed) && AppToApi.ContainsKey(parsed.Value);
}
