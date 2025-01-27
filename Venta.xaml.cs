namespace Mercader;

public partial class Venta : ContentPage
{
    public Venta()
    {
        InitializeComponent();
        CargarVentas();
    }
    private async void CargarVentas()
    {
        try
        {
            var ventas = await App.DataRepo.GetVentasAsync();
            VentasCollectionView.ItemsSource = ventas;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar los encargos: {ex.Message}");
        }
    }

}