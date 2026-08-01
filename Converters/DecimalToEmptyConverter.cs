using System.Globalization;

namespace Mercader.Converters;

/// <summary>
/// Convierte decimal 0 a string vacío (para que Entry muestre el Placeholder en vez de "0").
/// </summary>
public class DecimalToEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d && d == 0)
            return string.Empty;
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && decimal.TryParse(s, NumberStyles.Any, culture, out var result))
            return result;
        return 0m;
    }
}
