// <copyright file="InMemoryProductClientSurfaceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

/// <summary>
/// Broad surface coverage for InMemoryProductClient endpoints under-exercised by story Facts.
/// Use: High (CI / Sonar new_coverage). Scope: InMemoryProductClient.
/// </summary>
public sealed class InMemoryProductClientSurfaceTests
{
    /// <summary>
    /// Exercises portfolio, history, session, quote, money-move, receive, prefs, bootstrap, POS.
    /// Use: High. Scope: InMemoryProductClient happy paths.
    /// </summary>
    [Fact]
    public async Task InMemoryProductClient_HappyPaths_ReturnFixtures()
    {
        InMemoryProductClient api = new InMemoryProductClient();

        PortfolioDto portfolio = await api.GetPortfolioAsync(default);
        portfolio.TotalUsd.Should().NotBeNullOrWhiteSpace();
        portfolio.Holdings.Should().NotBeEmpty();

        IReadOnlyList<HistoryPointDto> history = await api.GetHistoryAsync("BTC", "1D", default);
        history.Should().NotBeEmpty();

        SessionDto session = await api.CreateSessionAsync(default);
        session.AccessToken.Should().NotBeNullOrWhiteSpace();

        QuoteDto quote = await api.GetQuoteAsync("USD", "BTC", default);
        quote.From.Should().Be("USD");
        quote.To.Should().Be("BTC");

        MoneyMoveDto convert = await api.ConvertAsync("USD", "BTC", "10", "idem-c", default);
        convert.Id.Should().NotBeNullOrWhiteSpace();
        convert.Status.Should().Be("pending");

        MoneyMoveDto transfer = await api.TransferAsync("bc1qdest", "0.01", "standard", "idem-t", default);
        transfer.Status.Should().Be("pending");

        MoneyMoveDto pay = await api.PayAsync(
            "5",
            new Dictionary<string, string> { ["USD"] = "5" },
            "idem-p",
            default);
        pay.Status.Should().Be("pending");

        ReceiveDto receive = await api.GetReceiveAsync("BTC", default);
        receive.Address.Should().NotBeNullOrWhiteSpace();
        receive.Asset.Should().Be("BTC");

        PrefsWireDto? prefs = await api.GetPrefsAsync(default);
        prefs.Should().NotBeNull();
        await api.PutPrefsAsync(prefs!, default);

        AccountBootstrapDto bootstrap = await api.GetAccountBootstrapAsync(default);
        bootstrap.Should().NotBeNull();

        PosSessionDto pos = await api.CreatePosSessionAsync(default);
        pos.SessionId.Should().NotBeNullOrWhiteSpace();
        PosSessionDto authorized = await api.AuthorizePosAsync(pos.SessionId, default);
        authorized.SessionId.Should().Be(pos.SessionId);
        PosSessionDto confirmed = await api.ConfirmPosAsync(pos.SessionId, default);
        confirmed.SessionId.Should().Be(pos.SessionId);

        IReadOnlyList<VaultBinaryDto> binaries = await api.GetVaultBinariesAsync(default);
        binaries.Should().NotBeNull();
    }
}
