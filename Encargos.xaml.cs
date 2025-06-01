namespace Mercader;

public partial class Encargos : ContentPage
{
    public Encargos()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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

    protected override bool OnBackButtonPressed()
    {
        // Devolver true previene la acción del botón atrás
        return true;
    }


    // Método para manejar el tap en lugar de selección
    private async void OnItemTapped(object sender, EventArgs e)
    {
        var frame = sender as Frame;
        var encargo = frame?.BindingContext as Encargo;

        if (encargo != null)
        {
            // Navegar a la página de detalles
            await Navigation.PushAsync(new DetalleEncargo(encargo));
        }
    }

    // Métodos para los SwipeItems
    private async void OnEditSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Encargo encargo)
        {
            await EditarEncargo(encargo);
        }
    }

    private async void OnDeleteSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Encargo encargo)
        {
            await EliminarEncargo(encargo);
        }
    }

    private async void OnDetallesSwipeItemInvoked(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var item = swipeItem?.BindingContext;
        if (item is Encargo encargo)
        {
            await MostrarDetalles(encargo);
        }
    }

    // Métodos auxiliares
    private async Task EditarEncargo(Encargo encargo)
    {
        try
        {
            await Navigation.PushAsync(new EditarEncargoPage(encargo));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de edición: {ex.Message}", "OK");
        }
    }

    private async Task EliminarEncargo(Encargo encargo)
    {
        bool confirm = await DisplayAlert("Confirmación", $"¿Realmente deseas eliminar el encargo de \"{encargo.Nombre}\" hecho el día: {encargo.Fecha}?", "Sí", "No");
        if (confirm)
        {
            try
            {
                await App.DataRepo.DeleteEncargoAsync(encargo);
                await DisplayAlert("Éxito", "Encargo eliminado correctamente", "OK");
                CargarEncargos();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar el encargo: {ex.Message}", "OK");
            }
        }
    }

    // Método para mostrar detalles
    private async Task MostrarDetalles(Encargo encargo)
    {
        try
        {
            await Navigation.PushAsync(new DetalleEncargo(encargo));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir la página de detalles: {ex.Message}", "OK");
        }
    }

    private async Task ConcretarVenta(Encargo encargo)
    {
        bool confirm = await DisplayAlert("Confirmación", $"¿Realmente deseas concretar la venta de {encargo.Descripcion} para \"{encargo.Nombre}\" pedido el día: {encargo.Fecha}?", "Sí", "No");
        if (confirm)
        {
            try
            {
                var venta = new Ventas
                {
                    Descripcion = encargo.Descripcion,
                    Precio = encargo.Precio,
                    Cantidad = encargo.Cantidad,
                    Fecha = DateTime.Now
                };

                await App.DataRepo.SaveVentasAsync(venta);
                await App.DataRepo.DeleteEncargoAsync(encargo);
                await DisplayAlert("Éxito", "Venta concretada correctamente", "OK");
                CargarEncargos();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al concretar la venta: {ex.Message}", "OK");
            }
        }
    }
}