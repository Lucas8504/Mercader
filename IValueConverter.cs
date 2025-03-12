using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Mercader
{
    public class AlternatingRowColorConverter : IValueConverter
    {
        // Colores para las filas alternas
        private readonly Color _evenColor = Color.FromArgb("#2a2a2a"); // Color base
        private readonly Color _oddColor = Color.FromArgb("#333333"); // Color alterno

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            // Manejo de valores nulos
            if (value == null)
                return _evenColor; // Color por defecto si el valor es nulo

            // Verificar si el valor es un índice (entero)
            if (value is int index)
            {
                // Alternar colores basados en el índice
                return index % 2 == 0 ? _evenColor : _oddColor;
            }

            // Si el valor no es un entero, devolver el color base
            return _evenColor;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            // ConvertBack no es necesario en este caso, pero debe implementarse
            throw new NotImplementedException();
        }
    }
}