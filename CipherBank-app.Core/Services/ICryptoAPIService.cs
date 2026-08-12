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
    Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken);

    Task<CryptoCurrency> GetCryptoPriceAsync(string symbol, CancellationToken cancellationToken);

    Task<PriceHistory> GetPriceHistoryAsync(string symbol, string period, CancellationToken cancellationToken);

    Task<List<CryptoCurrency>> SearchCryptoAsync(string query, CancellationToken cancellationToken);
}
