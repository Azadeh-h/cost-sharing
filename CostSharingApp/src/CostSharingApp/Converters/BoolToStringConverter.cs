
using System.Globalization;

namespace CostSharingApp.Converters;
/// <summary>
/// Converts a boolean value to a string based on provided parameters.
/// ConverterParameter format: "TrueValue,FalseValue"
/// Example: ConverterParameter="Edit Expense,Add Expense"
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string paramString)
        {
            return string.Empty;
        }

        var parts = paramString.Split(',');
        if (parts.Length != 2)
        {
            return string.Empty;
        }

        return boolValue ? parts[0] : parts[1];
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
