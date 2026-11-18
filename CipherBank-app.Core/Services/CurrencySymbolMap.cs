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
    private static readonly AssetSymbol ApiBitcoin = new("BITCOIN");
    private static readonly AssetSymbol ApiMonero = new("MONERO");

    // Keyed by AssetSymbol: record equality on the normalized ticker replaces the
    // case-insensitive string comparer.
    private static readonly Dictionary<AssetSymbol, AssetSymbol> AppToApi = new()
    {
        [AssetSymbol.Btc] = ApiBitcoin,
        [ApiBitcoin] = ApiBitcoin,
        [AssetSymbol.Xmr] = ApiMonero,
        [ApiMonero] = ApiMonero,
        [AssetSymbol.Usd] = AssetSymbol.Usd,
    };

    private static readonly Dictionary<AssetSymbol, AssetSymbol> ApiToApp = new()
    {
        [ApiBitcoin] = AssetSymbol.Btc,
        [ApiMonero] = AssetSymbol.Xmr,
        [AssetSymbol.Usd] = AssetSymbol.Usd,
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
        if (AppToApi.TryGetValue(appSymbol, out AssetSymbol? api))
        {
            return api.Value;
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
        return ApiToApp.TryGetValue(apiCurrency, out AssetSymbol? app) ? app : apiCurrency;
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
        => AssetSymbol.TryParse(symbol, out AssetSymbol? parsed) && AppToApi.ContainsKey(parsed);
}
