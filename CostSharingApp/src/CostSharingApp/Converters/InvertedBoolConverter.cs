// <copyright file="InvertedBoolConverter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>


using System.Globalization;

namespace CostSharingApp.Converters;
/// <summary>
/// Converts bool to inverted bool.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts bool to inverted bool.
    /// </summary>
    /// <param name="value">The bool value.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="parameter">Parameter.</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>Inverted bool.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return false;
    }

    /// <summary>
    /// Converts back (inverts again).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="parameter">Parameter.</param>
    /// <param name="culture">Culture.</param>
    /// <returns>Inverted bool.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return false;
    }
}
