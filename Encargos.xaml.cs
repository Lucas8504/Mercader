namespace Mercader;

public partial class Encargos : ContentPage
{
    public Encargos()
    {
        InitializeComponent();
        CargarEncargos();
    }

    private async void CargarEncargos()
    {
        try
        {
            var encargos = await App.DataRepo.GetEncargosAsync();
            EncargosCollectionView.ItemsSource = encargos;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar los encargos: {ex.Message}");
        }
    }


}

