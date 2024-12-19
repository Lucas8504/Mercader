using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Mercader;

public partial class VentaModal : ContentPage
{
    public VentaModal()
    {
        InitializeComponent();
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

            // Buscar la página principal
            var mainPage = Navigation.NavigationStack
                .OfType<MainPage>()
                .FirstOrDefault();

            if (mainPage != null)
            {
                mainPage.balance.Ventas.Add(venta);
                Console.WriteLine($"Venta agregada: Precio={venta.Precio}, Cantidad={venta.Cantidad}"); // Para debug
                mainPage.ActualizarEtiquetaVentas();
            }
            else
            {
                Console.WriteLine("No se encontró la página principal"); // Para debug
            }

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