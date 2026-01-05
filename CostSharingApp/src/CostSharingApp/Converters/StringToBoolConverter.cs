// <copyright file="StringToBoolConverter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CostSharingApp.Converters;

using System.Globalization;

/// <summary>
/// Converts string to bool (true if not empty).
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts string to bool.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="parameter">Parameter.</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>True if string is not null/empty.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string str && !string.IsNullOrWhiteSpace(str);
    }

    /// <summary>
    /// Not implemented - one-way converter.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="parameter">Parameter.</param>
    /// <param name="culture">Culture.</param>
    /// <returns>Not supported.</returns>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
