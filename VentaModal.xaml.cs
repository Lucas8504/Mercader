using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Mercader;

public partial class VentaModal : ContentPage
{
    private MainPage mainPage;  // Agregar esta línea

    public VentaModal(MainPage mainPage)  // Modificar el constructor
    {
        InitializeComponent();
        this.mainPage = mainPage;
    }

    private async void OnAgregarVentaClicked(object sender, EventArgs e)
    {
        try
        {
            var venta = new Ventas
            {
                Precio = decimal.Parse(PrecioEntry.Text),
                Cantidad = decimal.Parse(CantidadEntry.Text),
                Descripcion = DescripcionV_Entry.Text,
                Fecha = DateTime.Now
            };

            // Usar directamente la referencia a mainPage
            mainPage.balance.Ventas.Add(venta);
            mainPage.ActualizarEtiquetaVentas();

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