# Core Models

Domain models defined in `CipherBank-app.Core/Models/`. All are immutable records unless noted.

---

## AuthToken

**File**: `Models/AuthToken.cs`

Represents an authentication token with access and refresh tokens.

| Property | Type | Description |
|----------|------|-------------|
| AccessToken | string | JWT or opaque access token |
| RefreshToken | string | Token used to obtain a new access token |
| ExpiresUtc | DateTimeOffset | When the access token expires |

---

## CryptoCurrency

**File**: `Models/CryptoCurrency.cs`

Represents a cryptocurrency with current market data.

| Property | Type | Description |
|----------|------|-------------|
| Symbol | AssetSymbol | Ticker symbol (e.g., BTC, ETH) |
| Name | string | Full name |
| CurrentPrice | decimal | Current price in default currency |
| PriceChange24H | decimal | Absolute price change over 24h |
| PercentChange24H | decimal | Percentage change over 24h |
| MarketCap | decimal | Market capitalization |
| Volume24H | decimal | 24-hour trading volume |
| IconUrl | Uri? | URL for icon image |

**Computed properties**:

| Property | Type | Description |
|----------|------|-------------|
| IsPriceUp | bool | `PercentChange24H >= 0` |
| FormattedPrice | string | `"$" + CurrentPrice.ToString("N2", CultureInfo.InvariantCulture)` |
| FormattedPercentChange | string | e.g. `"+2.50%"` or `"-1.20%"` |

---

## PricePoint

**File**: `Models/PricePoint.cs`

A single data point in a price history series.

| Property | Type | Description |
|----------|------|-------------|
| Timestamp | DateTimeOffset | Time of the data point |
| Price | decimal | Price at that time |
| Volume | decimal? | Optional volume |

---

## PriceHistory

**File**: `Models/PriceHistory.cs`

Historical price data for a cryptocurrency over a time range.

| Property | Type | Description |
|----------|------|-------------|
| Symbol | AssetSymbol | Cryptocurrency symbol |
| PricePoints | List<PricePoint> | Ordered list of price points |
| StartDate | DateTimeOffset | Start of the period |
| EndDate | DateTimeOffset | End of the period |

**Computed properties**:

| Property | Type | Description |
|----------|------|-------------|
| HighPrice | decimal | Max price in series |
| LowPrice | decimal | Min price in series |
| AveragePrice | decimal | Average of all prices |
| PriceChange | decimal | Last price − first price |
| PercentChange | decimal | `(PriceChange / FirstPrice) * 100` |

---

## Transaction

**File**: `Models/Transaction.cs`

Represents a cryptocurrency transaction.

| Property | Type | Description |
|----------|------|-------------|
| Id | string | Unique transaction ID |
| Type | TransactionType | Purchase, Send, Receive, Exchange |
| Amount | decimal | Amount of crypto |
| CryptoSymbol | AssetSymbol | Symbol (e.g., BTC) |
| FromAddress | string? | Source address (if applicable) |
| ToAddress | string? | Destination address |
| Timestamp | DateTimeOffset | When the transaction occurred |
| Status | TransactionStatus | Pending, Confirmed, Failed, Cancelled |
| FeeAmount | decimal | Fee paid |

**Computed properties**:

| Property | Type | Description |
|----------|------|-------------|
| FormattedAmount | string | e.g. `"0.00100000 BTC"` |
| FormattedFee | string | Formatted fee |
| TypeDescription | string | Human-readable type |
| IsOutgoing | bool | `Type` is Send or Purchase |
| IsComplete | bool | `Status == Confirmed` |
| IsPending | bool | `Status == Pending` |

**JSON**: `Type` and `Status` use `JsonStringEnumConverter`.

---

## TransactionType

**File**: `Models/TransactionType.cs`

Enum for transaction type.

| Value | Description |
|-------|-------------|
| Purchase | Bought crypto |
| Send | Sent to another address |
| Receive | Received from another address |
| Exchange | Swapped between cryptos |

---

## TransactionStatus

**File**: `Models/TransactionStatus.cs`

Enum for transaction status.

| Value | Description |
|-------|-------------|
| Pending | Not yet confirmed |
| Confirmed | Completed on-chain |
| Failed | Transaction failed |
| Cancelled | User cancelled |

---

## Wallet

**File**: `Models/Wallet.cs`

Represents a user's cryptocurrency wallet.

| Property | Type | Description |
|----------|------|-------------|
| Id | string | Unique wallet ID |
| CryptoSymbol | AssetSymbol | Symbol (e.g., BTC) |
| CryptoName | string | Full name |
| Balance | decimal | Current balance |
| Address | string | Wallet address |
| CreatedAt | DateTimeOffset | Creation time |

**Computed properties**:

| Property | Type | Description |
|----------|------|-------------|
| FormattedBalance | string | e.g. `"0.50000000 BTC"` |
| HasBalance | bool | `Balance > 0` |

---

## AssetSymbol

**File**: `Models/AssetSymbol.cs`

Immutable ticker normalized for application identity. Construction trims and uppercases with the invariant culture. Blank input throws.

| Property | Type | Description |
|----------|------|-------------|
| Value | string | Normalized ticker |

Implicit conversion from `string`, plus `Parse` and `TryParse` (`TryParse` returns false for null or whitespace). `ToString()` returns `Value`.

**JSON**: `AssetSymbolJsonConverter` reads and writes the ticker as a string.

---

## PublicQuote

**File**: `Models/PublicQuote.cs`

Indicative quote from the public `/quote` or `/iquote` API. Not a server-honored lock.

| Property | Type | Description |
|----------|------|-------------|
| InputCurrency | AssetSymbol | Input ticker |
| InputAmount | decimal | Input amount |
| OutputCurrency | AssetSymbol | Output ticker |
| OutputAmount | decimal | Output amount |

**Computed properties**:

| Property | Type | Description |
|----------|------|-------------|
| Rate | decimal | `OutputAmount / InputAmount`, or `0` when input is `0` |
| InverseRate | decimal | `InputAmount / OutputAmount`, or `0` when output is `0` |

---

## LocalWalletDescriptor

**File**: `Models/LocalWalletDescriptor.cs`

Domain descriptor for one locally managed wallet.

| Property | Type | Description |
|----------|------|-------------|
| Id | string | Unique wallet ID |
| Symbol | AssetSymbol | Asset ticker |
| Label | string? | Optional display label |
| Address | string? | Optional address |
| Path | string? | Optional derivation path |
| AccountIndex | int | Account index |
| Kind | string | Wallet kind |
| CreatedAt | DateTimeOffset | Creation time |

---

## WalletCardItem

**File**: `Models/WalletCardItem.cs`

Presentation model pairing a wallet with current market data for the deck card.

| Property | Type | Description |
|----------|------|-------------|
| Wallet | Wallet | Underlying wallet |
| UsdValue | decimal | Balance valued in USD |
| PercentChange24H | decimal | Percentage change over 24h |

**Computed properties**:

| Property | Type | Description |
|----------|------|-------------|
| Symbol | AssetSymbol | `Wallet.CryptoSymbol` |
| Name | string | `Wallet.CryptoName` |
| FormattedBalance | string | `Wallet.FormattedBalance` |
| FormattedUsdValue | string | `"$" + UsdValue.ToString("N2", CultureInfo.InvariantCulture)` |
| IsPriceUp | bool | `PercentChange24H >= 0` |
| FormattedPercentChange | string | e.g. `"+2.50%"` or `"-1.20%"` |

`FromWallet(wallet, crypto)` sets `UsdValue` to `Balance * CurrentPrice` and copies `PercentChange24H`. `WithoutPrice(wallet)` uses zero USD value and zero percent change.
