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

    /// <summary>Connectivity probe for callers with no ambient token. Use: Low (diagnostics). Scope: IPublicQuoteService consumers.</summary>
    Task<bool> TestConnectionAsync() => TestConnectionAsync(CancellationToken.None);

    /// <summary>
    /// Returns supported currency codes as app tickers (BTC, XMR, USD, …).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered app symbols.</returns>
    Task<IReadOnlyList<string>> GetCurrenciesAsync(CancellationToken cancellationToken);

    /// <summary>Currency list for callers with no ambient token. Use: Medium (Convert load). Scope: IPublicQuoteService consumers.</summary>
    Task<IReadOnlyList<string>> GetCurrenciesAsync() => GetCurrenciesAsync(CancellationToken.None);

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

    /// <summary>Fixed-input quote for callers with no ambient token. Use: High (Convert typing). Scope: IPublicQuoteService consumers.</summary>
    Task<PublicQuote> GetInverseQuoteAsync(string inputSymbol, decimal inputAmount, string outputSymbol)
        => GetInverseQuoteAsync(inputSymbol, inputAmount, outputSymbol, CancellationToken.None);

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

    /// <summary>Fixed-output quote for callers with no ambient token. Use: High (Convert typing). Scope: IPublicQuoteService consumers.</summary>
    Task<PublicQuote> GetQuoteAsync(string inputSymbol, decimal outputAmount, string outputSymbol)
        => GetQuoteAsync(inputSymbol, outputAmount, outputSymbol, CancellationToken.None);
}
