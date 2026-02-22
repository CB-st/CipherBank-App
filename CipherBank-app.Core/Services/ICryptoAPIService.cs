using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for retrieving cryptocurrency market data and prices.
/// </summary>
public interface ICryptoAPIService
{
    Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken = default);
    Task<CryptoCurrency> GetCryptoPriceAsync(string symbol, CancellationToken cancellationToken = default);
    Task<PriceHistory> GetPriceHistoryAsync(string symbol, string period, CancellationToken cancellationToken = default);
    Task<List<CryptoCurrency>> SearchCryptoAsync(string query, CancellationToken cancellationToken = default);
}
