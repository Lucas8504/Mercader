namespace Mercader;

public partial class Gastos : ContentPage
{
    public Gastos()
    {
        InitializeComponent();
        CargarGastos();
    }

    private async void CargarGastos()
    {
        try
        {
            var gastos = await App.DataRepo.GetGastosAsync();
            GastosCollectionView.ItemsSource = gastos;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar los gastos: {ex.Message}");
        }
    }
}
