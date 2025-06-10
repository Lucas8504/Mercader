namespace Mercader;

public partial class Venta : ContentPage
{
    public Venta()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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
            Console.WriteLine($"Error al cargar las ventas: {ex.Message}");
        }
    }

    protected override bool OnBackButtonPressed()
    {
        // Devolver true previene la acción del botón atrás
        return true;
    }

    // Método para manejar el tap y navegar a DetalleVenta
    private async void OnItemTapped(object sender, EventArgs e)
    {
        var frame = sender as Frame;
        var venta = frame?.BindingContext as Ventas;
        if (venta != null)
        {
            try
            {
                await Navigation.PushAsync(new DetalleVenta(venta));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al abrir los detalles de la venta: {ex.Message}", "OK");
            }
        }
    }

    // Métodos para los SwipeItems
    private async void OnEditSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Ventas venta)
        {
            await EditarVenta(venta);
        }
    }

    private async void OnDeleteSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Ventas venta)
        {
            await EliminarVenta(venta);
        }
    }

    // Métodos auxiliares
    private async Task EditarVenta(Ventas venta)
    {
        try
        {
            await Navigation.PushAsync(new EditarVentaPage(venta));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
    }

    private async Task EliminarVenta(Ventas venta)
    {
        bool confirm = await DisplayAlert("Confirmación",
            $"¿Estás seguro de eliminar la venta \"{venta.Descripcion}\" del día {venta.Fecha:dd/MM/yyyy}?",
            "Sí", "No");

        if (confirm)
        {
            try
            {
                await App.DataRepo.DeleteVentaAsync(venta);
                await DisplayAlert("Éxito", "Venta eliminada correctamente", "OK");
                CargarVentas();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar la venta: {ex.Message}", "OK");
            }
        }
    }
}