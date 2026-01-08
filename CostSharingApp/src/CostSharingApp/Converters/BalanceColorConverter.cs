using System.Globalization;


namespace CostSharingApp.Converters;

/// <summary>
/// Converter that returns a color based on whether a balance is positive, negative, or zero.
/// </summary>
public class BalanceColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal balance)
        {
            if (balance > 0)
            {
                return Color.FromArgb("#198754"); // Success/Green
            }
            else if (balance < 0)
            {
                return Color.FromArgb("#DC3545"); // Danger/Red
            }
            else
            {
                return Color.FromArgb("#6C757D"); // Gray
            }
        }

        return Color.FromArgb("#000000"); // Black (fallback)
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
