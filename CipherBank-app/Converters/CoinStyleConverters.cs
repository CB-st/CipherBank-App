// <copyright file="CoinStyleConverters.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;

namespace CipherBank_app.Converters;

#pragma warning disable SA1649 // File name should match first type name - file intentionally groups both coin style converters
#pragma warning disable SA1402 // File may only contain a single type - both converters are Task 6's cohesive coin-styling pair

/// <summary>
/// Maps a crypto symbol to its brand color. Colors are injected from Colors.xaml at
/// registration time (see App.xaml) so token values are never duplicated here — the
/// same pattern as the existing BoolToColorConverter.
/// </summary>
public class CoinColorConverter : IValueConverter
{
    public Color BtcColor { get; set; } = Colors.Transparent;

    public Color EthColor { get; set; } = Colors.Transparent;

    public Color SolColor { get; set; } = Colors.Transparent;

    public Color DefaultColor { get; set; } = Colors.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.ToUpperInvariant() switch
        {
            "BTC" => BtcColor,
            "ETH" => EthColor,
            "SOL" => SolColor,
            _ => DefaultColor,
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Maps a crypto symbol to a display glyph for icon circles and card watermarks.
/// </summary>
public class CoinGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.ToUpperInvariant() switch
        {
            "BTC" => "₿",
            "ETH" => "◆",
            "SOL" => "◎",
            { Length: > 0 } symbol => symbol[..1],
            _ => "•",
        };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
