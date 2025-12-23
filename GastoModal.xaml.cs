using System.Globalization;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Mercader;

public partial class GastoModal : ContentPage
{
    private readonly MainPage _mainPage;
    private readonly DataRepository _repo;
    public Gasto Gasto { get; private set; } = null!; // Null forgiving operator

    public GastoModal(MainPage mainPage, DataRepository repo)
    {
        ArgumentNullException.ThrowIfNull(mainPage);
        ArgumentNullException.ThrowIfNull(repo);

        InitializeComponent();

        _mainPage = mainPage;
        _repo = repo;
    }

    private async void OnAgregarGastoClicked(object sender, EventArgs e)
    {
        if (!ValidateG_Entries())
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
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar gasto: {ex.Message}", "OK");
        }
        await SaveGastoAsync();
    }

    private async Task SaveGastoAsync()
    {
        _mainPage.balance.Gastos.Add(Gasto);
        await _repo.SaveGastoAsync(Gasto);
        _mainPage.ActualizarEtiquetaGastos();
        await Navigation.PopModalAsync();
    }

    private bool ValidateG_Entries()
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

        await Navigation.PopModalAsync();

    }
}