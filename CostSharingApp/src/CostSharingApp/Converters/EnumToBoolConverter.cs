// <copyright file="EnumToBoolConverter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>


using System.Globalization;

namespace CostSharingApp.Converters;
/// <summary>
/// Converts an enum value to boolean by comparing to a parameter.
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts enum to bool.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="parameter">The enum value to compare to (as string).</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>True if enum matches parameter.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        var enumString = value.ToString();
        var targetString = parameter.ToString();

        return string.Equals(enumString, targetString, StringComparison.OrdinalIgnoreCase);
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
