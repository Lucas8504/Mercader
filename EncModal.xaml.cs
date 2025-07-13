using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Mercader;

public partial class EncModal : ContentPage
{
    private readonly MainPage _mainPage;
    public Encargo Encargo { get; private set; } = null!; // Null forgiving operator

    public EncModal(MainPage mainPage)
    {
        ArgumentNullException.ThrowIfNull(mainPage);
        InitializeComponent();
        this._mainPage = mainPage;
    }

    private async void OnAgregarEncargoClicked(object sender, EventArgs e)
    {
        if (!ValidateE_Entries())
            return;

        try
        {
            Encargo = new Encargo
            {
                Nombre = EncargoEntry!.Text,
                Contacto = FormatPhoneNumber(ContactoEntry!.Text), // Formatear el teléfono
                Cantidad = decimal.Parse(CantidadEntry!.Text!),
                Precio = decimal.Parse(PrecioEntry!.Text!),
                Descripcion = DescripcionEntry.Text,
                FechaEntrega = FechaEntregaDatePicker.Date,
                Fecha = DateTime.Now
            };
        }
        catch (FormatException)
        {
            await DisplayAlert("Error", "Por favor, ingrese valores numéricos válidos", "OK");
            return;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar encargo: {ex.Message}", "OK");
            return;
        }

        await SaveEncargoAsync();
    }

    private async Task SaveEncargoAsync()
    {
        _mainPage.balance.Encargos.Add(Encargo);
        await App.DataRepo.SaveEncargoAsync(Encargo);
        _mainPage.ActualizarEtiquetaEncargos();
        await Navigation.PopModalAsync();
    }

    private bool ValidateE_Entries()
    {
        if (string.IsNullOrWhiteSpace(EncargoEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un nombre", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ContactoEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un número de teléfono", "OK");
            return false;
        }

        // Validación básica de formato de teléfono
        if (!IsValidPhoneNumber(ContactoEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un número de teléfono válido", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(DescripcionEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una descripción", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(PrecioEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un precio", "OK");
            return false;
        }

        if (!decimal.TryParse(PrecioEntry.Text, out decimal precio) || precio <= 0)
        {
            DisplayAlert("Error", "Por favor, ingrese un precio válido mayor a 0", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(CantidadEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una cantidad", "OK");
            return false;
        }

        if (!decimal.TryParse(CantidadEntry.Text, out decimal cantidad) || cantidad <= 0)
        {
            DisplayAlert("Error", "Por favor, ingrese una cantidad válida mayor a 0", "OK");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Valida que el número de teléfono tenga un formato básico válido
    /// </summary>
    /// <param name="phoneNumber">Número de teléfono a validar</param>
    /// <returns>True si el formato es válido, False en caso contrario</returns>
    private bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remover espacios, guiones, paréntesis y el signo +
        string cleanedNumber = phoneNumber.Replace(" ", "")
                                        .Replace("-", "")
                                        .Replace("(", "")
                                        .Replace(")", "")
                                        .Replace("+", "");

        // Verificar que solo contenga números y tenga entre 7 y 15 dígitos
        return cleanedNumber.All(char.IsDigit) &&
               cleanedNumber.Length >= 7 &&
               cleanedNumber.Length <= 15;
    }

    /// <summary>
    /// Formatea el número de teléfono según patrones comunes argentinos
    /// </summary>
    /// <param name="phoneNumber">Número de teléfono sin formatear</param>
    /// <returns>Número de teléfono formateado</returns>
    private string FormatPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Limpiar el número de caracteres especiales
        string cleanedNumber = phoneNumber.Replace(" ", "")
                                        .Replace("-", "")
                                        .Replace("(", "")
                                        .Replace(")", "")
                                        .Replace("+", "");

        // Formateo para números argentinos comunes
        if (cleanedNumber.Length == 10)
        {
            // Formato para celulares de Buenos Aires: 11 1234-5678
            if (cleanedNumber.StartsWith("11"))
            {
                return $"{cleanedNumber.Substring(0, 2)} {cleanedNumber.Substring(2, 4)}-{cleanedNumber.Substring(6)}";
            }
            // Formato para otros códigos de área: (0XXX) XXX-XXXX
            else if (cleanedNumber.StartsWith("0"))
            {
                return $"({cleanedNumber.Substring(0, 4)}) {cleanedNumber.Substring(4, 3)}-{cleanedNumber.Substring(7)}";
            }
        }

        // Formato para números con código de país argentino
        if (cleanedNumber.Length == 12 && cleanedNumber.StartsWith("54"))
        {
            return $"+54 {cleanedNumber.Substring(2, 2)} {cleanedNumber.Substring(4, 4)}-{cleanedNumber.Substring(8)}";
        }

        // Si no coincide con ningún patrón conocido, devolver el número limpio
        return cleanedNumber;
    }

    /// <summary>
    /// Evento para formatear el teléfono mientras el usuario escribe
    /// </summary>
    /// <param name="sender">Control que disparó el evento</param>
    /// <param name="e">Argumentos del evento</param>
    private void OnContactoTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            string oldText = e.OldTextValue ?? "";
            string newText = e.NewTextValue ?? "";

            // Solo formatear si el usuario está agregando texto (no borrando)
            if (newText.Length > oldText.Length)
            {
                // Remover el evento temporalmente para evitar loops infinitos
                entry.TextChanged -= OnContactoTextChanged;

                // Aplicar formato básico en tiempo real
                string formatted = ApplyBasicFormatting(newText);
                if (formatted != newText)
                {
                    entry.Text = formatted;
                    entry.CursorPosition = formatted.Length;
                }

                // Volver a agregar el evento
                entry.TextChanged += OnContactoTextChanged;
            }
        }
    }

    /// <summary>
    /// Aplica formato básico mientras el usuario escribe
    /// </summary>
    /// <param name="input">Texto ingresado por el usuario</param>
    /// <returns>Texto con formato básico aplicado</returns>
    private string ApplyBasicFormatting(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Remover todo excepto números
        string numbersOnly = new string(input.Where(char.IsDigit).ToArray());

        // Aplicar formato básico según la longitud
        return numbersOnly.Length switch
        {
            >= 11 when numbersOnly.StartsWith("11") =>
                $"{numbersOnly.Substring(0, 2)} {numbersOnly.Substring(2, Math.Min(4, numbersOnly.Length - 2))}" +
                (numbersOnly.Length > 6 ? $"-{numbersOnly.Substring(6)}" : ""),
            >= 8 =>
                $"{numbersOnly.Substring(0, Math.Min(4, numbersOnly.Length))} " +
                (numbersOnly.Length > 4 ? numbersOnly.Substring(4) : ""),
            _ => numbersOnly
        };
    }

    private async void Cancelar(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}