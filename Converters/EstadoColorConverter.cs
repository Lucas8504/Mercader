using System.Globalization;
using Microsoft.Maui.Controls;

namespace Mercader.Converters
{
    public class EstadoColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string estado = value as string ?? "PENDIENTE";
            return estado == "ENTREGADO" ? Color.FromArgb("#4CAF50") : Color.FromArgb("#FF9800");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
