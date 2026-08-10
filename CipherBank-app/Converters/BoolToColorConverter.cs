// <copyright file="BoolToColorConverter.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;

namespace CipherBank_app.Converters;

/// <summary>
/// Converts a boolean value to a Color.
/// Returns TrueColor (default: Green) when true, FalseColor (default: Red) when false.
/// </summary>
public sealed class BoolToColorConverter : BindableObject, IValueConverter
{
    public static readonly BindableProperty TrueColorProperty = BindableProperty.Create(
        nameof(TrueColor),
        typeof(Color),
        typeof(BoolToColorConverter),
        Colors.Green);

    public static readonly BindableProperty FalseColorProperty = BindableProperty.Create(
        nameof(FalseColor),
        typeof(Color),
        typeof(BoolToColorConverter),
        Colors.Red);

    public Color TrueColor
    {
        get => (Color)GetValue(TrueColorProperty);
        set => SetValue(TrueColorProperty, value);
    }

    public Color FalseColor
    {
        get => (Color)GetValue(FalseColorProperty);
        set => SetValue(FalseColorProperty, value);
    }

    /// <summary>Maps a boolean to the active themed color. Use: High. Scope: one bound status value.</summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueColor : FalseColor;

    /// <summary>Rejects reverse conversion because status colors are presentation-only. Use: Low. Scope: converter.</summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("Status colors cannot be converted back to a boolean.");
}
