// <copyright file="ObjectToBoolConverter.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;

namespace CipherBank_app.Converters;

/// <summary>
/// Converts any object value to a boolean indicating whether it is non-null.
/// Use this for non-string types (e.g., Wallet?, CryptoCurrency?) instead of StringToBoolConverter.
/// </summary>
public class ObjectToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
