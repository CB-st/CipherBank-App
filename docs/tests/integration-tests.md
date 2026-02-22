# Integration Tests

**Project**: CipherBank-app.IntegrationTests

Tests API endpoint behavior using WireMock. Verifies that the app correctly handles HTTP responses and security patterns.

## Structure

```
CipherBank-app.IntegrationTests/
├── MockServerFixture.cs    # WireMock server + endpoint setup
├── ApiIntegrationTests.cs  # API endpoint tests
└── SecurityTests.cs       # Auth, rate limiting, injection tests
```

## Dependencies

- **CipherBank-app.Core** – Models for deserialization
- **WireMock.Net** – In-process mock HTTP server
- **FluentAssertions** – Assertions
- **xUnit** – Test framework

## MockServerFixture

`IClassFixture<MockServerFixture>` provides a shared WireMock server and HttpClient.

**Endpoints configured**:

| Path | Method | Response |
|------|--------|----------|
| /health | GET | 200, `{"status":"healthy"}` |
| /auth/login | POST | 200, token JSON |
| /auth/refresh | POST | 200, refreshed token |
| /api/v1/crypto/prices | GET | 200, BTC/ETH/SOL list |
| /api/v1/crypto/price/BTC | GET | 200, single crypto |
| /api/v1/crypto/search | GET | 200, search results |
| /api/v1/wallets | GET | 200, wallet list |
| /api/v1/wallets | POST | 201, new wallet |
| /api/v1/transactions | GET | 200, transaction list |
| /api/v1/transactions/purchase | POST | 200, purchase tx |
| /api/v1/transactions/send | POST | 200, send tx |

## ApiIntegrationTests

| Test | Description |
|------|-------------|
| HealthCheck_ReturnsOk | GET /health → 200 |
| Login_WithValidCredentials_ReturnsToken | POST /auth/login → token |
| RefreshToken_WithValidToken_ReturnsNewToken | POST /auth/refresh → refreshed token |
| GetCryptoPrices_ReturnsListOfCryptos | GET /api/v1/crypto/prices → list |
| GetWallets_ReturnsList | GET /api/v1/wallets → list |
| CreateWallet_ReturnsCreated | POST /api/v1/wallets → 201 |
| GetTransactionHistory_ReturnsList | GET /api/v1/transactions?walletId= → list |
| PurchaseCrypto_ReturnsTransaction | POST /api/v1/transactions/purchase → tx |
| SendCrypto_ReturnsTransaction | POST /api/v1/transactions/send → tx |

## SecurityTests

| Test | Description |
|------|-------------|
| UnauthorizedRequest_ReturnsUnauthorized | Request without auth → 401 |
| AuthorizedRequest_WithValidToken_Succeeds | Request with Bearer token → 200 |
| ExpiredToken_Returns401 | Expired token → 401 |
| InvalidCredentials_ReturnsUnauthorized | Invalid login → 401 |
| RateLimiting_Returns429WhenExceeded | Rate-limited endpoint → 429, Retry-After |
| XssAttempt_IsSanitized | XSS in query → 400 |
| SqlInjectionAttempt_IsRejected | SQL injection in path → 400 |
| ContentSecurityHeaders_ArePresent | Secure headers present |
| SensitiveDataNotInLogs_TransactionEndpoint | Send endpoint accepts request |
| TimeoutHandling_ReturnsGatewayTimeout | Slow endpoint → 504 |

**Note**: SecurityTests configures additional WireMock scenarios per test; the fixture's default endpoints are extended or overridden.
