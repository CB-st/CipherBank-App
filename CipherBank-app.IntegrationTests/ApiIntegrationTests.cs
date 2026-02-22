using System.Net;
using System.Net.Http.Json;
using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.IntegrationTests;

/// <summary>
/// Integration tests that verify API endpoint behavior using WireMock.
/// These tests ensure that the application correctly handles API responses.
/// </summary>
public class ApiIntegrationTests : IClassFixture<MockServerFixture>
{
    private readonly MockServerFixture _fixture;
    private readonly HttpClient _client;

    public ApiIntegrationTests(MockServerFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.HttpClient;
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new { user = "testuser", password = "password123" };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await response.Content.ReadFromJsonAsync<AuthToken>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrEmpty();
        token.RefreshToken.Should().NotBeNullOrEmpty();
        token.ExpiresUtc.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var refreshRequest = new { refreshToken = "old_refresh_token" };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await response.Content.ReadFromJsonAsync<AuthToken>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().Contain("refreshed");
    }

    [Fact]
    public async Task GetCryptoPrices_ReturnsListOfCryptos()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/crypto/prices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cryptos = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>();
        cryptos.Should().NotBeNull();
        cryptos.Should().HaveCountGreaterThan(0);
        cryptos.Should().Contain(c => c.Symbol == "BTC");
    }

    [Fact]
    public async Task GetCryptoPrice_WithValidSymbol_ReturnsCrypto()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/crypto/price/BTC");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var crypto = await response.Content.ReadFromJsonAsync<CryptoCurrency>();
        crypto.Should().NotBeNull();
        crypto!.Symbol.Should().Be("BTC");
        crypto.Name.Should().Be("Bitcoin");
        crypto.CurrentPrice.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchCrypto_WithQuery_ReturnsResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/crypto/search?q=bit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var cryptos = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>();
        cryptos.Should().NotBeNull();
        cryptos.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetWallets_ReturnsUserWallets()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/wallets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var wallets = await response.Content.ReadFromJsonAsync<List<Wallet>>();
        wallets.Should().NotBeNull();
        wallets.Should().HaveCountGreaterThan(0);
        wallets.Should().Contain(w => w.CryptoSymbol == "BTC");
    }

    [Fact]
    public async Task CreateWallet_ReturnsNewWallet()
    {
        // Arrange
        var createRequest = new { cryptoSymbol = "SOL" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/wallets", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var wallet = await response.Content.ReadFromJsonAsync<Wallet>();
        wallet.Should().NotBeNull();
        wallet!.CryptoSymbol.Should().Be("SOL");
        wallet.Balance.Should().Be(0m);
        wallet.Address.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTransactionHistory_ReturnsTransactions()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/transactions?walletId=wallet_btc_001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>();
        transactions.Should().NotBeNull();
        transactions.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task PurchaseCrypto_ReturnsTransaction()
    {
        // Arrange
        var purchaseRequest = new { symbol = "BTC", amount = 0.1m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/purchase", purchaseRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transaction = await response.Content.ReadFromJsonAsync<Transaction>();
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Purchase);
        transaction.CryptoSymbol.Should().Be("BTC");
    }

    [Fact]
    public async Task SendCrypto_ReturnsTransaction()
    {
        // Arrange
        var sendRequest = new { fromWalletId = "wallet_btc_001", toAddress = "bc1qrecipient", amount = 0.05m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions/send", sendRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transaction = await response.Content.ReadFromJsonAsync<Transaction>();
        transaction.Should().NotBeNull();
        transaction!.Type.Should().Be(TransactionType.Send);
        transaction.Status.Should().Be(TransactionStatus.Pending);
    }
}
