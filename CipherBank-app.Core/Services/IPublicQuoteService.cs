// <copyright file="IPublicQuoteService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Client for the live CipherBank public quote surface
/// (<c>/currencies</c>, <c>/quote</c>, <c>/iquote</c>, <c>/test</c>).
/// </summary>
public interface IPublicQuoteService
{
    /// <summary>
    /// Probes API connectivity via <c>POST /test</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the API accepts the connectivity test.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns supported currency codes as app tickers (BTC, XMR, USD, …).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered app symbols.</returns>
    Task<IReadOnlyList<string>> GetCurrenciesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Calculates output for a fixed input amount (<c>POST /iquote</c>).
    /// </summary>
    /// <param name="inputSymbol">App ticker for the input currency.</param>
    /// <param name="inputAmount">Input amount.</param>
    /// <param name="outputSymbol">App ticker for the output currency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Indicative quote.</returns>
    Task<PublicQuote> GetInverseQuoteAsync(
        string inputSymbol,
        decimal inputAmount,
        string outputSymbol,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calculates input required for a fixed output amount (<c>POST /quote</c>).
    /// </summary>
    /// <param name="inputSymbol">App ticker for the input currency.</param>
    /// <param name="outputAmount">Desired output amount.</param>
    /// <param name="outputSymbol">App ticker for the output currency.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Indicative quote.</returns>
    Task<PublicQuote> GetQuoteAsync(
        string inputSymbol,
        decimal outputAmount,
        string outputSymbol,
        CancellationToken cancellationToken);
}
