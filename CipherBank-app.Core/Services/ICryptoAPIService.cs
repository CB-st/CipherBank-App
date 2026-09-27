// <copyright file="ICryptoAPIService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for retrieving cryptocurrency market data and prices.
/// </summary>
public interface ICryptoApiService
{
    /// <summary>Returns current market data for all available assets.</summary>
    /// <returns>The available asset market data.</returns>
    Task<List<CryptoCurrency>> GetCryptoPricesAsync() =>
        GetCryptoPricesAsync(CancellationToken.None);

    /// <summary>Returns current market data for all available assets.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The available asset market data.</returns>
    Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken);

    /// <summary>Returns current market data for <paramref name="symbol"/>.</summary>
    /// <param name="symbol">Asset to retrieve.</param>
    /// <returns>The asset market data.</returns>
    Task<CryptoCurrency> GetCryptoPriceAsync(AssetSymbol symbol) =>
        GetCryptoPriceAsync(symbol, CancellationToken.None);

    /// <summary>Returns current market data for <paramref name="symbol"/>.</summary>
    /// <param name="symbol">Asset to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The asset market data.</returns>
    Task<CryptoCurrency> GetCryptoPriceAsync(
        AssetSymbol symbol,
        CancellationToken cancellationToken);

    /// <summary>Returns price history for an asset and period.</summary>
    /// <param name="symbol">Asset to retrieve.</param>
    /// <param name="period">Requested history period.</param>
    /// <returns>The asset price history.</returns>
    Task<PriceHistory> GetPriceHistoryAsync(AssetSymbol symbol, string period) =>
        GetPriceHistoryAsync(symbol, period, CancellationToken.None);

    /// <summary>Returns price history for an asset and period.</summary>
    /// <param name="symbol">Asset to retrieve.</param>
    /// <param name="period">Requested history period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The asset price history.</returns>
    Task<PriceHistory> GetPriceHistoryAsync(
        AssetSymbol symbol,
        string period,
        CancellationToken cancellationToken);

    /// <summary>Searches available assets using <paramref name="query"/>.</summary>
    /// <param name="query">Search text.</param>
    /// <returns>Matching asset market data.</returns>
    Task<List<CryptoCurrency>> SearchCryptoAsync(string query) =>
        SearchCryptoAsync(query, CancellationToken.None);

    /// <summary>Searches available assets using <paramref name="query"/>.</summary>
    /// <param name="query">Search text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching asset market data.</returns>
    Task<List<CryptoCurrency>> SearchCryptoAsync(
        string query,
        CancellationToken cancellationToken);
}
