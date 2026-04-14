using System.Globalization;
using Mercader.Models.Domain;
using Mercader.ViewModels;
#if ANDROID
using Mercader.Platforms.Android;
#endif
using Microsoft.Maui.Controls;

namespace Mercader;

public partial class GastoModal : ContentPage
{
    public Gasto Gasto { get; private set; } = null!;

    private readonly MainViewModel _viewModel;

    public GastoModal(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        _viewModel = viewModel;
    }

    private async void OnAgregarGastoClicked(object sender, EventArgs e)
    {
        if (!ValidateEntries())
            return;

        try
        {
            Gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Cantidad = decimal.Parse(CantidadG_Entry!.Text!, CultureInfo.InvariantCulture),
                Monto = decimal.Parse(MontoGastoEntry!.Text!, CultureInfo.InvariantCulture),
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
            await DisplayAlert("Error", $"Error al agregar gasto: {ex.Message}", "OK");
            return;
        }

        await _viewModel.AddGastoAsync(Gasto);
#if ANDROID
        KeyboardHelper.Close();
#endif
        MessagingCenter.Send<object>(this, "DataChanged");
        await Navigation.PopModalAsync();
    }

    private bool ValidateEntries()
    {
        if (string.IsNullOrWhiteSpace(DescripcionGastoEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese una descripción", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(MontoGastoEntry.Text))
        {
            DisplayAlert("Error", "Por favor, ingrese un Monto", "OK");
            return false;
        }
        if (string.IsNullOrWhiteSpace(CantidadG_Entry.Text))
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
