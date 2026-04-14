using System.Globalization;
using Mercader.Models.Domain;
using Mercader.ViewModels;
#if ANDROID
using Mercader.Platforms.Android;
#endif
using Microsoft.Maui.Controls;

namespace Mercader;

public partial class EncModal : ContentPage
{
    public Encargo Encargo { get; private set; } = null!;

    private readonly MainViewModel _viewModel;

    public EncModal(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        _viewModel = viewModel;
    }

    private async void OnAgregarEncargoClicked(object sender, EventArgs e)
    {
        if (!ValidateEntries())
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
        await _viewModel.AddEncargoAsync(Encargo);
#if ANDROID
        KeyboardHelper.Close();
#endif
        MessagingCenter.Send<object>(this, "DataChanged");
        await Navigation.PopModalAsync();
    }

    private bool ValidateEntries()
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

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        string cleanedNumber = phoneNumber.Replace(" ", "")
                                        .Replace("-", "")
                                        .Replace("(", "")
                                        .Replace(")", "")
                                        .Replace("+", "");

        return cleanedNumber.All(char.IsDigit) &&
               cleanedNumber.Length >= 7 &&
               cleanedNumber.Length <= 15;
    }

    private string CleanPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

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
