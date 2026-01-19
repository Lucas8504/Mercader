using System.Globalization;
#if ANDROID
using Mercader.Platforms.Android;
#endif

namespace Mercader;

public partial class EncModal : ContentPage
{
    
    public Encargo Encargo { get; private set; } = null!; // Null forgiving operator

    private readonly DataRepository _repo;

    public EncModal(DataRepository repo)
    {
        
        ArgumentNullException.ThrowIfNull(repo);

        InitializeComponent();
        _repo = repo;
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
                Contacto = CleanPhoneNumber(ContactoEntry!.Text),
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
        
        await _repo.SaveEncargoAsync(Encargo);

#if ANDROID
        KeyboardHelper.Close();
#endif

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
    /// Limpia el número de teléfono removiendo caracteres especiales
    /// </summary>
    /// <param name="phoneNumber">Número de teléfono a limpiar</param>
    /// <returns>Número de teléfono solo con dígitos</returns>
    private string CleanPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Remover todos los caracteres que no sean números
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    private async void Cancelar(object sender, EventArgs e)
    {
#if ANDROID
        KeyboardHelper.Close();
#endif

        await Navigation.PopModalAsync();
    }
}