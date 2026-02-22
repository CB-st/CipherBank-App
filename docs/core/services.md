# Core Services

Interfaces and utility classes in `CipherBank-app.Core/Services/`.

---

## IAuthService

**File**: `Services/IAuthService.cs`

Service for user authentication and token management.

| Method | Returns | Description |
|--------|---------|-------------|
| LoginAsync(user, password, ct?) | Task<AuthToken> | Authenticate and return tokens |
| RefreshAsync(refreshToken, ct?) | Task<AuthToken> | Refresh access token |
| GetStoredTokenAsync() | Task<AuthToken?> | Get stored token from secure storage |
| IsTokenExpiredAsync() | Task<bool> | Check if current token is expired |
| LogoutAsync() | Task | Clear stored tokens |
| RevokeTokenAsync(ct?) | Task<bool> | Revoke tokens on server before logout |

---

## ICryptoApiService

**File**: `Services/ICryptoApiService.cs`

Interface naming uses PascalCase "Api" (ICryptoApiService). Service for cryptocurrency market data and prices.

| Method | Returns | Description |
|--------|---------|-------------|
| GetCryptoPricesAsync(ct?) | Task<List<CryptoCurrency>> | Current prices for all supported cryptos |
| GetCryptoPriceAsync(symbol, ct?) | Task<CryptoCurrency> | Price for a specific symbol |
| GetPriceHistoryAsync(symbol, period, ct?) | Task<PriceHistory> | Historical prices (e.g. period: "1d", "7d", "30d", "1y") |
| SearchCryptoAsync(query, ct?) | Task<List<CryptoCurrency>> | Search by name or symbol |

---

## IWalletService

**File**: `Services/IWalletService.cs`

Service for managing cryptocurrency wallets.

| Method | Returns | Description |
|--------|---------|-------------|
| GetWalletsAsync(ct?) | Task<List<Wallet>> | All user wallets |
| GetWalletAsync(id, ct?) | Task<Wallet> | Single wallet by ID |
| GetWalletBalanceAsync(id, ct?) | Task<decimal> | Balance for a wallet |
| CreateWalletAsync(cryptoSymbol, ct?) | Task<Wallet> | Create a new wallet |

---

## ITransactionService

**File**: `Services/ITransactionService.cs`

Service for managing cryptocurrency transactions.

| Method | Returns | Description |
|--------|---------|-------------|
| GetTransactionHistoryAsync(walletId, ct?) | Task<List<Transaction>> | Transaction history for a wallet |
| PurchaseCryptoAsync(symbol, amount, ct?) | Task<Transaction> | Purchase crypto |
| SendCryptoAsync(fromWalletId, toAddress, amount, ct?) | Task<Transaction> | Send crypto to address |
| GetTransactionStatusAsync(transactionId, ct?) | Task<TransactionStatus> | Status of a transaction |

---

## RateLimiter

**File**: `Services/RateLimiter.cs`

Thread-safe sliding-window rate limiter. Limits requests to a configurable number per time window.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| MaxRequests | int | 60 | Max requests per window |
| WindowDuration | TimeSpan | 1 minute | Window size |
| CurrentRequestCount | int | - | Current count in window (read-only) |

| Method | Returns | Description |
|--------|---------|-------------|
| TryAcquireAsync(ct?) | Task<bool> | Acquire permit; returns false if rate limited |
| GetWaitTimeAsync(ct?) | Task<TimeSpan> | Time to wait before next request; Zero if allowed |

---

## AddressValidator

**File**: `Services/Validation/AddressValidator.cs`

Static validator for cryptocurrency addresses. Supports Bitcoin, Ethereum, and Solana formats.

| Method | Returns | Description |
|--------|---------|-------------|
| IsValidAddress(address, symbol) | bool | Validate by symbol (BTC, ETH, SOL, or generic) |
| IsValidBitcoinAddress(address) | bool | P2PKH (1...), P2SH (3...), Bech32 (bc1...), testnet |
| IsValidEthereumAddress(address) | bool | 0x + 40 hex chars |
| IsValidSolanaAddress(address) | bool | Base58, 32–44 chars |

---

## LogRedactionHelper

**File**: `Services/Logging/LogRedactionHelper.cs`

Static helper for redacting sensitive data in log messages.

| Method | Returns | Description |
|--------|---------|-------------|
| RedactUsername(username) | string | e.g. "testuser123" → "t*********3" |
| RedactWalletId(walletId) | string | e.g. "wallet123...cdef" |
| RedactAddress(address) | string | e.g. "1A1zP1...fNa" |
| RedactToken(token) | string | e.g. "eyJhbGci..." |
| RedactEmail(email) | string | e.g. "us***@example.com" |
| RedactTransactionId(txId) | string | e.g. "tx_12345...cdef" |
| Redact(value, showChars) | string | Generic redaction with configurable visible chars |
