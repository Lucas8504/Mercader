using System.Globalization;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Mercader;

public partial class GastoModal : ContentPage
{
    private readonly MainPage _mainPage;
    public Gasto Gasto { get; private set; } = null!; // Null forgiving operator

    public GastoModal(MainPage mainPage)
    {
        ArgumentNullException.ThrowIfNull(mainPage);


        InitializeComponent();
        _mainPage = mainPage;

        Gasto = new()
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
            Gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Cantidad = decimal.Parse(CantidadG_Entry!.Text!, CultureInfo.InvariantCulture),
                Monto = decimal.Parse(MontoGastoEntry!.Text!, CultureInfo.InvariantCulture),
                Fecha = DateTime.Now
            };
            await SaveGastoAsync();
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
        _mainPage.balance.Gastos.Add(Gasto);
        await App.DataRepo.SaveGastoAsync(Gasto);
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