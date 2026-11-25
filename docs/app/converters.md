# Value Converters

Implement `IValueConverter`. Registered in `App.xaml` `ResourceDictionary`.

---

## BoolToColorConverter

**File**: `Converters/BoolToColorConverter.cs`

**App.xaml key**: `PriceChangeColorConverter`, `StatusColorConverter` (with custom TrueColor/FalseColor)

| Property | Type | Default |
|----------|------|---------|
| TrueColor | Color | Green |
| FalseColor | Color | Red |

**Convert**: `bool` → `Color` (true → TrueColor, false → FalseColor). Non-bool → FalseColor.

**ConvertBack**: Not implemented.

**Usage**: Price change color (green/red), status background (success/error).

---

## InvertedBoolConverter

**File**: `Converters/InvertedBoolConverter.cs`

**App.xaml key**: `InvertedBoolConverter`

**Convert**: `bool` → `!bool`. Non-bool → false.

**ConvertBack**: `bool` → `!bool`. Non-bool → false.

**Usage**: `IsEnabled="{Binding IsBusy, Converter={StaticResource InvertedBoolConverter}}"` – disable when busy.

---

## StringToBoolConverter

**File**: `Converters/StringToBoolConverter.cs`

**App.xaml key**: `StringToBoolConverter`

**Convert**: `string` → `!string.IsNullOrWhiteSpace(stringValue)`. Non-string → false.

**ConvertBack**: Not implemented.

**Usage**: `IsVisible="{Binding ErrorMessage, Converter={StaticResource StringToBoolConverter}}"` – show when there is an error message.

---

## ObjectToBoolConverter

**File**: `Converters/ObjectToBoolConverter.cs`

**App.xaml key**: `ObjectToBoolConverter`

**Convert**: any value → `true` when non-null, `false` when null.

**ConvertBack**: Not implemented.

**Usage**: `IsVisible="{Binding SelectedCrypto, Converter={StaticResource ObjectToBoolConverter}}"` on `PurchasePage`, and `IsVisible="{Binding SelectedWallet, Converter={StaticResource ObjectToBoolConverter}}"` on `WalletPage` – show when a purchase asset or wallet is selected.

---

## CoinColorConverter

**File**: `Converters/CoinStyleConverters.cs`

**App.xaml key**: `CoinColorConverter` (`BtcColor` = `CoinBtc`, `EthColor` = `CoinEth`, `SolColor` = `CoinSol`, `DefaultColor` = `CoinDefault`)

| Property | Type | Default |
|----------|------|---------|
| BtcColor | Color | Transparent |
| EthColor | Color | Transparent |
| SolColor | Color | Transparent |
| DefaultColor | Color | Transparent |

**Convert**: `AssetSymbol` → `Color` (`BTC` → BtcColor, `ETH` → EthColor, `SOL` → SolColor, any other ticker → DefaultColor). Non-`AssetSymbol` → DefaultColor.

**ConvertBack**: Not supported.

**Usage**: `BackgroundColor="{Binding Symbol, Converter={StaticResource CoinColorConverter}}"` – coin-circle color on `WalletPage`, `PurchasePage`, `DashboardPage`, and `AssetPickerPage`.

---

## CoinGlyphConverter

**File**: `Converters/CoinStyleConverters.cs`

**App.xaml key**: `CoinGlyphConverter`

**Convert**: `AssetSymbol` → glyph (`BTC` → `₿`, `ETH` → `◆`, `SOL` → `◎`, any other non-empty ticker → its first character). Non-`AssetSymbol` or empty value → `•`.

**ConvertBack**: Not supported.

**Usage**: `Text="{Binding Symbol, Converter={StaticResource CoinGlyphConverter}}"` – watermark and coin-circle glyph on `WalletPage`, `PurchasePage`, `DashboardPage`, and `AssetPickerPage`.
