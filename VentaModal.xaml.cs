using System.Globalization;

namespace Mercader;

public partial class VentaModal : ContentPage
{
    private readonly MainPage _mainPage;
    private Ventas _venta = null!; // Null forgiving operator


    public VentaModal(MainPage mainPage)  // Modificar el constructor
    {
        ArgumentNullException.ThrowIfNull(mainPage);

        InitializeComponent();
        _mainPage = mainPage;

        _venta = new()
        {
            Descripcion = string.Empty,
            Precio = 0,
            Cantidad = 0,
            Fecha = DateTime.Now
        };
    }


    private async void OnAgregarVentaClicked(object sender, EventArgs e)
    {
        if (!ValidateEntries())
            return;


        try
        {
            _venta = new()
            {
                Precio = decimal.Parse(PrecioEntry!.Text!, CultureInfo.InvariantCulture),
                Cantidad = decimal.Parse(CantidadEntry!.Text!, CultureInfo.InvariantCulture),
                Descripcion = DescripcionV_Entry!.Text!,
                Fecha = DateTime.Now
            };

            await SaveVentaAsync();
        }
        catch (FormatException)
        {
            await DisplayAlert("Error", "Por favor, ingrese valores numéricos válidos", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al agregar venta: {ex.Message}", "OK");
        }
    }

    private async Task SaveVentaAsync()
    {
        _mainPage.balance.Ventas.Add(_venta);
        await App.DataRepo.SaveVentasAsync(_venta);
        _mainPage.ActualizarEtiquetaVentas();
        await Navigation.PopModalAsync();
    }

    private bool ValidateEntries() =>
        !string.IsNullOrWhiteSpace(PrecioEntry?.Text) &&
        !string.IsNullOrWhiteSpace(CantidadEntry?.Text) &&
        !string.IsNullOrWhiteSpace(DescripcionV_Entry?.Text);


    private async void Cancelar(object sender, EventArgs e)
    {

        await Navigation.PopModalAsync();

    }

}