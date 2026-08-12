// <copyright file="InMemoryProductClient.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;

namespace CipherBank_app.V1;

/// <summary>Stateful in-memory product client for development and integration fixtures.</summary>
public sealed class InMemoryProductClient : IProductClient
{
    // --- Fixture constants ---
    private const string MockAccessToken = "mock-access";
    private const string MockRefreshToken = "mock-refresh";
    private const int QuoteTtlSeconds = 30;
    private const int HistoryDayCount = 30;
    private const int HistoryHourlyPointCount = 24;
    private const int HistoryWeeklyPointCount = 7;
    private const int HistoryMonthlyPointCount = 30;
    private const int HistoryQuarterlyPointCount = 90;
    private const int HistoryYearlyPointCount = 52;
    private const int HistoryStepSecondsHourly = 3600;
    private const int HistoryStepSecondsDaily = 86400;
    private const int HistoryStepSecondsWeekly = 86400 * 7;
    private const double HistoryBaseValue = 100;
    private const double HistoryWavePeriod = 3.0;
    private const double HistoryWaveAmplitude = 2.0;
    private const double HistoryWaveOffset = 0.3;
    private const int SessionExpiresHours = 1;
    private const int MockChallengeIdSuffixLength = 8;
    private const int MockKeyShareIdSuffixLength = 8;
    private const int MockWalletIdSuffixLength = 12;
    private const int MockCardIdSuffixLength = 12;
    private const int MockPosTokenSuffixLength = 12;
    private const int MockChallengeCiphertextBytes = 48;
    private const int MockX25519PublicKeyBytes = 32;
    private const int MockMlKemCiphertextBytes = 1088;
    private const int MockManagedAddressSuffixLength = 10;
    private const int PosAuthorizedTtlMs = 60_000;
    private const int PosReadyTtlMs = 45_000;
    private const int DefaultAppLockIdleSeconds = 120;
    private const long MockBootstrapSyncedAtUnixMs = 1_720_900_000_000L;
    private const string MockReceiveAddress = "bc1qmockreceiveaddress0000000000000000";

    private readonly TimeProvider _timeProvider;
    private readonly List<VaultCardDto> _vaultCards =
    [
        new() { CardId = "card_lab_1", Last4 = "4242", Brand = "visa", Label = "Hardware test", HardwareTest = true },
    ];

    private PrefsWireDto _prefs;

    public InMemoryProductClient()
        : this(TimeProvider.System)
    {
    }

    public InMemoryProductClient(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _prefs = CreateDefaultPrefs();
    }

    public Task<PortfolioDto> GetPortfolioAsync(CancellationToken ct)
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

    public Task<IReadOnlyList<HistoryPointDto>> GetHistoryAsync(string symbol, string range, CancellationToken ct)
    {
        long now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        (int points, int stepSeconds) = ResolveHistoryShape(range);
        List<HistoryPointDto> pts = new List<HistoryPointDto>(points + 1);
        double v = HistoryBaseValue;
        for (int i = points; i >= 0; i--)
        {
            v += (Math.Sin(i / HistoryWavePeriod) * HistoryWaveAmplitude) + HistoryWaveOffset;
            pts.Add(new HistoryPointDto { T = now - (i * (long)stepSeconds), V = v });
        }

        return Task.FromResult<IReadOnlyList<HistoryPointDto>>(pts);
    }

    public Task<SessionDto> CreateSessionAsync(CancellationToken ct)
        => Task.FromResult(new SessionDto
        {
            AccessToken = MockAccessToken,
            RefreshToken = MockRefreshToken,
            ExpiresAt = _timeProvider.GetUtcNow().AddHours(SessionExpiresHours).ToUnixTimeMilliseconds(),
        });

    public Task<SessionChallengeDto> CreateSessionChallengeAsync(string accountPublicKeyWire, CancellationToken ct)
    {
        // Prefer ISessionChallengeClient / IPqChannelChallengeSource in DI for real crypto.
        // This stub satisfies IProductClient for callers that hit InMemoryProductClient directly.
        _ = accountPublicKeyWire;
        return Task.FromResult(new SessionChallengeDto
        {
            ChallengeId = "ch_mock_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..MockChallengeIdSuffixLength],
            Ciphertext = Convert.ToBase64String(new byte[MockChallengeCiphertextBytes]),
            ApiPublicKey = Convert.ToBase64String(new byte[MockX25519PublicKeyBytes]),
            ApiKeyId = "api_mock",
            Algorithm = "x25519-chacha20poly1305",
        });
    }

    public Task<KeyShareResponseDto> EstablishKeyShareAsync(KeyShareRequestDto request, CancellationToken ct)
    {
        _ = request;
        return Task.FromResult(new KeyShareResponseDto
        {
            KeyShareId = "ks_mock_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..MockKeyShareIdSuffixLength],
            MlKemCiphertext = Convert.ToBase64String(new byte[MockMlKemCiphertextBytes]),
            ServerX25519PublicKey = Convert.ToBase64String(new byte[MockX25519PublicKeyBytes]),
            Algorithm = "hybrid-mlkem768-x25519-v1",
        });
    }

    public Task<CreateWalletResultDto> CreateWalletAsync(CreateWalletRequestDto request, CancellationToken ct)
    {
        string modeKey = string.IsNullOrWhiteSpace(request.Mode) ? "MANAGED" : request.Mode.ToUpperInvariant();
        string mode = modeKey switch
        {
            "MANAGED" => "managed",
            "WATCH" => "watch",
            _ => request.Mode.Trim(),
        };
        string symbol = string.IsNullOrWhiteSpace(request.Symbol) ? "XMR" : request.Symbol.ToUpperInvariant();
        string label = string.IsNullOrWhiteSpace(request.Label) ? $"CipherBank {mode}" : request.Label;
        string walletId = "wlt_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..MockWalletIdSuffixLength];
        string? address = modeKey switch
        {
            "MANAGED" => "4" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..MockManagedAddressSuffixLength],
            "WATCH" => request.Address,
            _ => request.Address,
        };
        return Task.FromResult(new CreateWalletResultDto
        {
            WalletId = walletId,
            Symbol = symbol,
            Label = label,
            Mode = mode,
            Address = address,
        });
    }

    public Task<QuoteDto> GetQuoteAsync(string from, string toAsset, CancellationToken ct)
        => Task.FromResult(new QuoteDto
        {
            From = from.ToUpperInvariant(),
            To = toAsset.ToUpperInvariant(),
            Rate = from.Equals("BTC", StringComparison.OrdinalIgnoreCase) ? "66000" : "1.00",
            ExpiresAt = _timeProvider.GetUtcNow().AddSeconds(QuoteTtlSeconds).ToUnixTimeMilliseconds(),
        });

    public Task<MoneyMoveDto> ConvertAsync(string from, string toAsset, string amount, string idempotencyKey, CancellationToken ct)
        => Task.FromResult(new MoneyMoveDto { Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), Status = "pending" });

    public Task<MoneyMoveDto> TransferAsync(string destination, string amount, string speed, string idempotencyKey, CancellationToken ct)
        => Task.FromResult(new MoneyMoveDto { Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), Status = "pending" });

    public Task<MoneyMoveDto> PayAsync(string amount, IReadOnlyDictionary<string, string> mix, string idempotencyKey, CancellationToken ct)
        => Task.FromResult(new MoneyMoveDto { Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), Status = "pending" });

    public Task<ReceiveDto> GetReceiveAsync(string asset, CancellationToken ct)
        => Task.FromResult(new ReceiveDto { Asset = asset.ToUpperInvariant(), Address = MockReceiveAddress, Uri = null });

    public Task<IReadOnlyList<VaultBinaryDto>> GetVaultBinariesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<VaultBinaryDto>>(new[]
        {
            new VaultBinaryDto { BinaryId = "bin_xmr_1", Label = "XMR wallet-rpc shard", Kind = "wallet_rpc" },
        });

    public Task<IReadOnlyList<VaultCardDto>> GetVaultCardsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<VaultCardDto>>(_vaultCards.ToList());
    }

    public Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _ = idempotencyKey;
        VaultCardDto added = new VaultCardDto
        {
            CardId = string.IsNullOrWhiteSpace(card.CardId) ? "card_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..MockCardIdSuffixLength] : card.CardId,
            Last4 = card.Last4,
            Brand = card.Brand,
            Label = card.Label,
            HardwareTest = card.HardwareTest,
        };
        _vaultCards.Add(added);
        return Task.FromResult(added);
    }

    public Task DeleteVaultCardAsync(string cardId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _vaultCards.RemoveAll(card => card.CardId == cardId);
        return Task.CompletedTask;
    }

    public Task<PosSessionDto> CreatePosSessionAsync(CancellationToken ct)
        => Task.FromResult(new PosSessionDto { SessionId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture), Status = "pending_auth" });

    public Task<PosSessionDto> AuthorizePosAsync(string sessionId, CancellationToken ct)
        => Task.FromResult(new PosSessionDto
        {
            SessionId = sessionId,
            Status = "authorized",
            TokenRef = "tok_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..MockPosTokenSuffixLength],
            Last4 = "4242",
            Brand = "visa",
            TtlMs = PosAuthorizedTtlMs,
        });

    public Task<PosSessionDto> ConfirmPosAsync(string sessionId, CancellationToken ct)
        => Task.FromResult(new PosSessionDto
        {
            SessionId = sessionId,
            Status = "ready_to_present",
            TokenRef = "tok_ready",
            Last4 = "4242",
            Brand = "visa",
            TtlMs = PosReadyTtlMs,
        });

    public Task<PrefsWireDto?> GetPrefsAsync(CancellationToken ct)
        => Task.FromResult<PrefsWireDto?>(_prefs);

    public Task PutPrefsAsync(PrefsWireDto prefs, CancellationToken ct)
    {
        _prefs = prefs;
        return Task.CompletedTask;
    }

    public Task<AccountBootstrapDto> GetAccountBootstrapAsync(CancellationToken ct)
        => Task.FromResult(new AccountBootstrapDto
        {
            Prefs = new PrefsWireDto
            {
                DefaultSendSpeed = "instant",
                CoraEnabled = true,
            },
            Recipients =
            {
                CreateBootstrapRecipient(
                    "maya",
                    "Maya Chen",
                    "Maya Chen",
                    "Chase",
                    "4021",
                    "021000021",
                    memo: null),
                CreateBootstrapRecipient(
                    "sunset",
                    "Sunset Property Mgmt",
                    "Sunset Property Management LLC",
                    "Wells Fargo",
                    "5544",
                    "121000248",
                    memo: "Rent"),
            },
            SyncedAt = MockBootstrapSyncedAtUnixMs,
        });

    /// <summary>
    /// Builds a fixture bootstrap recipient (shared shape for mock twins).
    /// Use: Low (GetAccountBootstrapAsync). Scope: InMemoryProductClient.
    /// </summary>
    private static BootstrapRecipientDto CreateBootstrapRecipient(
        string id,
        string displayName,
        string holder,
        string bank,
        string last4,
        string routing,
        string? memo)
        => new()
        {
            Id = id,
            DisplayName = displayName,
            AccountHolderName = holder,
            BankName = bank,
            AccountLast4 = last4,
            AccountType = "checking",
            RoutingNumber = routing,
            Memo = memo,
        };

    private static (int Points, int StepSeconds) ResolveHistoryShape(string range)
    {
        return range.Trim().ToUpperInvariant() switch
        {
            "1D" => (HistoryHourlyPointCount, HistoryStepSecondsHourly),
            "1W" or "7D" => (HistoryWeeklyPointCount, HistoryStepSecondsDaily),
            "1M" or "30D" => (HistoryMonthlyPointCount, HistoryStepSecondsDaily),
            "90D" => (HistoryQuarterlyPointCount, HistoryStepSecondsDaily),
            "1Y" or "ALL" => (HistoryYearlyPointCount, HistoryStepSecondsWeekly),
            _ => (HistoryDayCount, HistoryStepSecondsDaily),
        };
    }

    private static PrefsWireDto CreateDefaultPrefs()
    {
        PrefsWireDto prefs = new()
        {
            ValuesHiddenOnLaunch = false,
            CoraEnabled = true,
            DefaultSendSpeed = "instant",
            Appearance = "dark",
            BaseCurrency = "USD",
            LockIdleSeconds = DefaultAppLockIdleSeconds,
            AssetsLayout = "separate",
        };
        prefs.ReplaceHomeOrder(["cora", "balance", "quickActions", "performance", "holdings", "localWallets"]);
        prefs.ReplaceHomeVisible(new Dictionary<string, bool>
        {
            ["cora"] = true,
            ["balance"] = true,
            ["quickActions"] = true,
            ["performance"] = true,
            ["holdings"] = true,
            ["localWallets"] = true,
        });
        return prefs;
    }
}
