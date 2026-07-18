// <copyright file="MockProductApi.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.V1;

/// <summary>In-process /v1 mock (Cora fixtures parity).</summary>
public sealed class MockProductApi : IProductApi
{
    // --- Fixture constants ---
    private const string MockAccessToken = "mock-access";
    private const string MockRefreshToken = "mock-refresh";
    private const int QuoteTtlSeconds = 30;
    private const int HistoryDayCount = 30;
    private const string MockReceiveAddress = "bc1qmockreceiveaddress0000000000000000";

    public Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct = default)
        => Task.FromResult(new PortfolioDto
        {
            TotalUsd = "128450.22",
            Change24HPct = "2.14",
            Holdings =
            {
                new HoldingDto { Symbol = "BTC", Name = "Bitcoin", Balance = "1.25000000", UsdValue = "82500.00", Change24HPct = "1.8" },
                new HoldingDto { Symbol = "ETH", Name = "Ethereum", Balance = "12.50000000", UsdValue = "31250.00", Change24HPct = "2.4" },
                new HoldingDto { Symbol = "USD", Name = "US Dollar", Balance = "14700.22", UsdValue = "14700.22", Change24HPct = "0" },
            },
        });

    public Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct = default)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var pts = new List<HistoryPointDto>();
        double v = 100;
        for (int i = HistoryDayCount; i >= 0; i--)
        {
            v += Math.Sin(i / 3.0) * 2 + 0.3;
            pts.Add(new HistoryPointDto { T = now - (i * 86400), V = v });
        }

        return Task.FromResult<IReadOnlyList<HistoryPointDto>>(pts);
    }

    public Task<SessionDto> CreateSessionAsync(CancellationToken ct = default)
        => Task.FromResult(new SessionDto
        {
            AccessToken = MockAccessToken,
            RefreshToken = MockRefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds(),
        });

    public Task<QuoteDto> GetQuoteAsync(string from, string to, CancellationToken ct = default)
        => Task.FromResult(new QuoteDto
        {
            From = from.ToUpperInvariant(),
            To = to.ToUpperInvariant(),
            Rate = from.Equals("BTC", StringComparison.OrdinalIgnoreCase) ? "66000" : "1.00",
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(QuoteTtlSeconds).ToUnixTimeMilliseconds(),
        });

    public Task<MoneyMoveDto> ConvertAsync(string from, string to, string amount, string idempotencyKey, CancellationToken ct = default)
        => Task.FromResult(new MoneyMoveDto { Id = Guid.NewGuid().ToString("N"), Status = "pending" });

    public Task<MoneyMoveDto> TransferAsync(string to, string amount, string speed, string idempotencyKey, CancellationToken ct = default)
        => Task.FromResult(new MoneyMoveDto { Id = Guid.NewGuid().ToString("N"), Status = "pending" });

    public Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct = default)
        => Task.FromResult(new MoneyMoveDto { Id = Guid.NewGuid().ToString("N"), Status = "pending" });

    public Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct = default)
        => Task.FromResult(new ReceiveDto { Asset = asset.ToUpperInvariant(), Address = MockReceiveAddress, Uri = null });

    public Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<VaultBinaryDto>>(new[]
        {
            new VaultBinaryDto { BinaryId = "bin_xmr_1", Label = "XMR wallet-rpc shard", Kind = "wallet_rpc" },
        });

    public Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<VaultCardDto>>(new[]
        {
            new VaultCardDto { CardId = "card_lab_1", Last4 = "4242", Brand = "visa", Label = "Hardware test", HardwareTest = true },
        });

    public Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct = default)
        => Task.FromResult(new PosSessionDto { SessionId = Guid.NewGuid().ToString("N"), Status = "pending_auth" });

    public Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult(new PosSessionDto
        {
            SessionId = sessionId,
            Status = "authorized",
            TokenRef = "tok_" + Guid.NewGuid().ToString("N")[..12],
            Last4 = "4242",
            Brand = "visa",
            TtlMs = 60_000,
        });

    public Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult(new PosSessionDto
        {
            SessionId = sessionId,
            Status = "ready_to_present",
            TokenRef = "tok_ready",
            Last4 = "4242",
            Brand = "visa",
            TtlMs = 45_000,
        });
}
