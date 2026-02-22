using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for retrieving cryptocurrency market data and prices.
/// </summary>
public interface ICryptoApiService
{
    /// <summary>
    /// Gets the current prices for all supported cryptocurrencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of cryptocurrencies with current market data.</returns>
    Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current price for a specific cryptocurrency.
    /// </summary>
    /// <param name="symbol">The cryptocurrency symbol (e.g., BTC, ETH).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cryptocurrency with current market data.</returns>
    Task<CryptoCurrency> GetCryptoPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the price history for a cryptocurrency over a specified period.
    /// </summary>
    /// <param name="symbol">The cryptocurrency symbol.</param>
    /// <param name="period">The time period (e.g., "1d", "7d", "30d", "1y").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The price history data.</returns>
    Task<PriceHistory> GetPriceHistoryAsync(string symbol, string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for cryptocurrencies matching the query.
    /// </summary>
    /// <param name="query">The search query (name or symbol).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of matching cryptocurrencies.</returns>
    Task<List<CryptoCurrency>> SearchCryptoAsync(string query, CancellationToken cancellationToken = default);
}
