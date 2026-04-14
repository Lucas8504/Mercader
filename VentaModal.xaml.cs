using System.Globalization;
using Mercader.Models.Domain;
using Mercader.ViewModels;
#if ANDROID
using Mercader.Platforms.Android;
#endif
using Microsoft.Maui.Controls;

namespace Mercader;

public partial class VentaModal : ContentPage
{
    public Ventas Venta { get; private set; } = null!;

    private readonly MainViewModel _viewModel;

    public VentaModal(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        _viewModel = viewModel;
    }

    private async void OnAgregarVentaClicked(object sender, EventArgs e)
    {
        if (!ValidateEntries())
            return;

        try
        {
            Venta = new Ventas
            {
                Precio = decimal.Parse(PrecioEntry!.Text!, CultureInfo.InvariantCulture),
                Cantidad = decimal.Parse(CantidadEntry!.Text!, CultureInfo.InvariantCulture),
                Descripcion = DescripcionV_Entry!.Text!,
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
            await DisplayAlert("Error", $"Error al agregar venta: {ex.Message}", "OK");
            return;
        }

        await _viewModel.AddVentaAsync(Venta);
#if ANDROID
        KeyboardHelper.Close();
#endif
        MessagingCenter.Send<object>(this, "DataChanged");
        await Navigation.PopModalAsync();
    }

    private bool ValidateEntries()
    {
        if (string.IsNullOrWhiteSpace(DescripcionV_Entry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una descripción", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(PrecioEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un precio", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(CantidadEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una cantidad", "OK");
            return false;
        }

        return true;
    }

    private async void Cancelar(object sender, EventArgs e)
    {
#if ANDROID
        KeyboardHelper.Close();
#endif
        await Navigation.PopModalAsync();
    }
}
