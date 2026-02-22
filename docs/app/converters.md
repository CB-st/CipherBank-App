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
