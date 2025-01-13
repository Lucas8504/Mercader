using System.Globalization;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Mercader;

public partial class GastoModal : ContentPage
{
    private readonly MainPage _mainPage;
    private Gasto _gasto = null!; // Null forgiving operator

    public GastoModal(MainPage mainPage)
    {
        ArgumentNullException.ThrowIfNull(mainPage);


        InitializeComponent();
        this._mainPage = mainPage;

        _gasto = new()
        {
            Descripcion = string.Empty,
            Monto = 0,
            Cantidad = 0,
            Fecha = DateTime.Now
        };
    }

    private async void OnAgregarGastoClicked(object sender, EventArgs e)
    {
        if (!ValidateGEntries())
            return;

        try
        {
            _gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Cantidad = decimal.Parse(CantidadG_Entry!.Text!, CultureInfo.InvariantCulture),
                Monto = decimal.Parse(MontoGastoEntry!.Text!, CultureInfo.InvariantCulture),
                Fecha = DateTime.Now
            };
            // Usar directamente la referencia a mainPage
            _mainPage.balance.Gastos.Add(_gasto);
            _mainPage.ActualizarEtiquetaGastos();
            await Navigation.PopModalAsync();
        }
        catch (FormatException)
        {
            await DisplayAlert("Error", "Por favor, ingrese valores numéricos válidos", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar gasto: {ex.Message}", "OK");
        }

    }

    private async Task SaveGastoAsync()
    {
        _mainPage.balance.Gastos.Add(_gasto);
        await App.DataRepo.SaveGastoAsync(_gasto);
        _mainPage.ActualizarEtiquetaGastos();
        await Navigation.PopModalAsync();
    }

    private bool ValidateGEntries() =>
        !string.IsNullOrWhiteSpace(MontoGastoEntry?.Text) &&
        !string.IsNullOrWhiteSpace(CantidadG_Entry?.Text) &&
        !string.IsNullOrWhiteSpace(DescripcionGastoEntry?.Text);


    private async void Cancelar(object sender, EventArgs e)
    {

        await Navigation.PopModalAsync();

    }
}