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
        try
        {
            var gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Monto = decimal.Parse(MontoGastoEntry.Text),
                Fecha = DateTime.Now
            };
            // Usar directamente la referencia a mainPage
            _mainPage.balance.Gastos.Add(gasto);
            _mainPage.ActualizarEtiquetaGastos();
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar venta: {ex.Message}", "OK");
        }

    }


    private async void Cancelar(object sender, EventArgs e)
    {

        await Navigation.PopModalAsync();

    }
}