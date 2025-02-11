using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace Mercader
{
    public class GridLengthToDoubleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is GridLength gridLength)
            {
                return gridLength.Value;
            }
            return 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return new GridLength(doubleValue);
            }
            return GridLength.Auto;
        }
    }
}
