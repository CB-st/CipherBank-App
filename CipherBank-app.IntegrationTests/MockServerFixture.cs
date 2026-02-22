using System.Net;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CipherBank_app.IntegrationTests;

/// <summary>
/// Fixture for managing WireMock server for integration tests.
/// Provides realistic mock API responses for testing service integrations.
/// </summary>
public class MockServerFixture : IDisposable
{
    public WireMockServer Server { get; }
    public string BaseUrl => Server.Url!;
    public HttpClient HttpClient { get; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public MockServerFixture()
    {
        Server = WireMockServer.Start();
        HttpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        SetupDefaultEndpoints();
    }

    private void SetupDefaultEndpoints()
    {
        // Health check endpoint
        Server.Given(Request.Create()
                .WithPath("/health")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{\"status\":\"healthy\"}"));

        // Auth endpoints
        SetupAuthEndpoints();

        // Crypto API endpoints
        SetupCryptoEndpoints();

        // Wallet endpoints
        SetupWalletEndpoints();

        // Transaction endpoints
        SetupTransactionEndpoints();
    }

    private void SetupAuthEndpoints()
    {
        // Login endpoint
        Server.Given(Request.Create()
                .WithPath("/auth/login")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    accessToken = "test_access_token_12345",
                    refreshToken = "test_refresh_token_67890",
                    expiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                }));

        // Refresh endpoint
        Server.Given(Request.Create()
                .WithPath("/auth/refresh")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    accessToken = "refreshed_access_token",
                    refreshToken = "refreshed_refresh_token",
                    expiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                }));
    }

    private void SetupCryptoEndpoints()
    {
        var cryptoPrices = new[]
        {
            new { symbol = "BTC", name = "Bitcoin", currentPrice = 97500.00m, priceChange24h = 1250.50m, percentChange24h = 1.30m, marketCap = 1920000000000m, volume24h = 45000000000m, iconUrl = "https://example.com/btc.png" },
            new { symbol = "ETH", name = "Ethereum", currentPrice = 3450.00m, priceChange24h = -45.25m, percentChange24h = -1.29m, marketCap = 415000000000m, volume24h = 18000000000m, iconUrl = "https://example.com/eth.png" },
            new { symbol = "SOL", name = "Solana", currentPrice = 195.00m, priceChange24h = 8.45m, percentChange24h = 4.53m, marketCap = 92000000000m, volume24h = 5500000000m, iconUrl = "https://example.com/sol.png" }
        };

        // Get all prices
        Server.Given(Request.Create()
                .WithPath("/api/v1/crypto/prices")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(cryptoPrices));

        // Get single price
        Server.Given(Request.Create()
                .WithPath("/api/v1/crypto/price/BTC")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(cryptoPrices[0]));

        // Search endpoint
        Server.Given(Request.Create()
                .WithPath("/api/v1/crypto/search")
                .WithParam("q")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(cryptoPrices));
    }

    private void SetupWalletEndpoints()
    {
        var wallets = new[]
        {
            new { id = "wallet_btc_001", cryptoSymbol = "BTC", cryptoName = "Bitcoin", balance = 0.52483921m, address = "bc1qtest123456789", createdAt = DateTimeOffset.UtcNow.AddDays(-30) },
            new { id = "wallet_eth_001", cryptoSymbol = "ETH", cryptoName = "Ethereum", balance = 3.84729184m, address = "0xtest123456789", createdAt = DateTimeOffset.UtcNow.AddDays(-20) }
        };

        // Get all wallets
        Server.Given(Request.Create()
                .WithPath("/api/v1/wallets")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(wallets));

        // Create wallet
        Server.Given(Request.Create()
                .WithPath("/api/v1/wallets")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Created)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = "wallet_new_001",
                    cryptoSymbol = "SOL",
                    cryptoName = "Solana",
                    balance = 0m,
                    address = "newsolanaaddress123",
                    createdAt = DateTimeOffset.UtcNow
                }));
    }

    private void SetupTransactionEndpoints()
    {
        var transactions = new[]
        {
            new { id = "tx_001", type = "Purchase", amount = 0.1m, cryptoSymbol = "BTC", fromAddress = (string?)null, toAddress = "bc1qtest", timestamp = DateTimeOffset.UtcNow.AddDays(-5), status = "Confirmed", feeAmount = 0.0015m },
            new { id = "tx_002", type = "Send", amount = 0.05m, cryptoSymbol = "BTC", fromAddress = "bc1qtest", toAddress = "bc1qother", timestamp = DateTimeOffset.UtcNow.AddDays(-2), status = "Confirmed", feeAmount = 0.00005m }
        };

        // Get transaction history
        Server.Given(Request.Create()
                .WithPath("/api/v1/transactions")
                .WithParam("walletId")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(transactions));

        // Purchase crypto
        Server.Given(Request.Create()
                .WithPath("/api/v1/transactions/purchase")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = "tx_purchase_new",
                    type = "Purchase",
                    amount = 0.1m,
                    cryptoSymbol = "BTC",
                    fromAddress = (string?)null,
                    toAddress = "bc1qmywallet",
                    timestamp = DateTimeOffset.UtcNow,
                    status = "Confirmed",
                    feeAmount = 0.0015m
                }));

        // Send crypto
        Server.Given(Request.Create()
                .WithPath("/api/v1/transactions/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBodyAsJson(new
                {
                    id = "tx_send_new",
                    type = "Send",
                    amount = 0.05m,
                    cryptoSymbol = "BTC",
                    fromAddress = "bc1qmywallet",
                    toAddress = "bc1qrecipient",
                    timestamp = DateTimeOffset.UtcNow,
                    status = "Pending",
                    feeAmount = 0.00005m
                }));
    }

    public void Dispose()
    {
        HttpClient.Dispose();
        Server.Stop();
        Server.Dispose();
        GC.SuppressFinalize(this);
    }
}
